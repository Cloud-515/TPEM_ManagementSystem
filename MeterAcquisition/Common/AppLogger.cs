using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Tpem.Diagnostics
{
    /// <summary>
    /// 日志级别。
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Off = 4
    }

    /// <summary>
    /// 零依赖的滚动文件日志器（线程安全、异步落盘、按天+按大小滚动）。
    ///
    /// 为什么不用 NLog/Serilog：本解决方案是 .NET Framework 4.8 + packages.config，
    /// 当前构建环境只有 dotnet SDK（不还原 packages.config 依赖），引入新包会破坏可构建性。
    /// 本类 API 刻意与主流日志库同形，后续替换只需改本类内部实现，调用方无需改动。
    ///
    /// 对应改进项：P0-8（全项目零日志）。
    /// </summary>
    public static class AppLogger
    {
        /// <summary>队列上限。写盘跟不上时丢弃最新记录并计数，绝不阻塞采集线程。</summary>
        private const int MaxQueueLength = 20000;

        /// <summary>单个日志文件大小上限，超出后追加序号滚动。</summary>
        private const long MaxFileBytes = 10L * 1024 * 1024;

        /// <summary>日志保留天数，启动时清理更早的文件。</summary>
        private const int MaxRetainedDays = 30;

        private static readonly BlockingCollection<string> Queue =
            new BlockingCollection<string>(new ConcurrentQueue<string>(), MaxQueueLength);

        private static readonly object SyncRoot = new object();

        private static Thread _writerThread;
        private static string _logDirectory;
        private static string _auditDirectory;
        private static string _component = "app";
        private static LogLevel _minimumLevel = LogLevel.Info;
        private static bool _initialized;
        private static bool _mirrorToConsole;
        private static long _droppedCount;
        private static string _currentFilePath;
        private static DateTime _currentFileDate;
        private static int _currentFileIndex;
        /// <summary>当前日志目录，供界面"打开日志目录"使用。</summary>
        public static string LogDirectory
        {
            get { return _logDirectory; }
        }

        /// <summary>因队列满而丢弃的记录数，用于自监控。</summary>
        public static long DroppedCount
        {
            get { return Interlocked.Read(ref _droppedCount); }
        }

        /// <summary>
        /// 初始化日志器。可重复调用，只有第一次生效。
        /// </summary>
        /// <param name="component">组件名，作为文件名前缀（如 acquisition / ingestion）。</param>
        /// <param name="minimumLevel">最低记录级别。</param>
        /// <param name="mirrorToConsole">是否同时输出到控制台（Worker 用 true）。</param>
        public static void Initialize(string component, LogLevel minimumLevel, bool mirrorToConsole)
        {
            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                _component = string.IsNullOrWhiteSpace(component) ? "app" : component.Trim();
                _minimumLevel = minimumLevel;
                _mirrorToConsole = mirrorToConsole;
                _logDirectory = ResolveWritableDirectory("logs");
                _auditDirectory = ResolveWritableDirectory("audit");
                _initialized = true;

                _writerThread = new Thread(WriterLoop);
                _writerThread.IsBackground = true;
                _writerThread.Name = "AppLogger";
                _writerThread.Start();
            }

            TryCleanupOldFiles();
            Info("Logger", "日志已启动，目录: " + (_logDirectory ?? "(不可用)") + "，级别: " + minimumLevel);
        }

        public static void Debug(string category, string message)
        {
            Write(LogLevel.Debug, category, message, null);
        }

        public static void Info(string category, string message)
        {
            Write(LogLevel.Info, category, message, null);
        }
        public static void Warn(string category, string message)
        {
            Write(LogLevel.Warn, category, message, null);
        }

        public static void Warn(string category, string message, Exception exception)
        {
            Write(LogLevel.Warn, category, message, exception);
        }

        public static void Error(string category, string message)
        {
            Write(LogLevel.Error, category, message, null);
        }

        public static void Error(string category, string message, Exception exception)
        {
            Write(LogLevel.Error, category, message, exception);
        }

        /// <summary>
        /// 操作审计。用于分合闸、写参数、清零等会改变现场设备状态的动作。
        /// 审计记录同时写主日志和独立审计文件，并且是**同步落盘**的
        /// —— 遥控命令下发后即使进程立刻崩溃，记录也必须存在。
        /// 对应改进项：P0-4。
        /// </summary>
        public static void Audit(string action, string target, bool success, string detail)
        {
            var line = string.Format(
                CultureInfo.InvariantCulture,
                "{0} | user={1}@{2} | action={3} | target={4} | result={5} | {6}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                SafeUserName(),
                SafeMachineName(),
                action,
                target,
                success ? "OK" : "FAIL",
                detail ?? string.Empty);

            Write(LogLevel.Info, "Audit", line, null);

            if (string.IsNullOrEmpty(_auditDirectory))
            {
                return;
            }

            try
            {
                var path = Path.Combine(
                    _auditDirectory,
                    "audit-" + DateTime.Now.ToString("yyyyMM", CultureInfo.InvariantCulture) + ".log");
                File.AppendAllText(path, line + Environment.NewLine, new UTF8Encoding(false));
            }
            catch
            {
                // 审计文件不可写不应阻断现场操作，主日志里已有同一条记录。
            }
        }
        /// <summary>
        /// 停止日志器并把队列里剩余的记录刷盘。退出流程里必须调用。
        /// </summary>
        public static void Shutdown()
        {
            Thread writer;
            lock (SyncRoot)
            {
                if (!_initialized)
                {
                    return;
                }

                writer = _writerThread;
            }

            var dropped = DroppedCount;
            if (dropped > 0)
            {
                Write(LogLevel.Warn, "Logger", "本次运行因队列满丢弃日志 " + dropped + " 条。", null);
            }

            Write(LogLevel.Info, "Logger", "日志已停止。", null);

            try
            {
                Queue.CompleteAdding();
                if (writer != null)
                {
                    writer.Join(TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
            }
        }

        private static void Write(LogLevel level, string category, string message, Exception exception)
        {
            if (!_initialized || level < _minimumLevel || _minimumLevel == LogLevel.Off)
            {
                return;
            }

            var builder = new StringBuilder(160);
            builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            builder.Append(" [").Append(LevelTag(level)).Append("]");
            builder.Append(" [").Append(string.IsNullOrEmpty(category) ? "-" : category).Append("]");
            builder.Append(" [T").Append(Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture)).Append("] ");
            builder.Append(message ?? string.Empty);

            if (exception != null)
            {
                builder.AppendLine();
                builder.Append("    ").Append(exception.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
            }

            Enqueue(builder.ToString());
        }
        private static void Enqueue(string line)
        {
            if (_mirrorToConsole)
            {
                try
                {
                    Console.WriteLine(line);
                }
                catch
                {
                }
            }

            try
            {
                if (!Queue.IsAddingCompleted && Queue.TryAdd(line))
                {
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                // 队列已关闭（Shutdown 之后），直接丢弃。
                return;
            }

            Interlocked.Increment(ref _droppedCount);
        }

        private static void WriterLoop()
        {
            try
            {
                foreach (var line in Queue.GetConsumingEnumerable())
                {
                    AppendLine(line);
                }
            }
            catch
            {
                // 写线程绝不能把异常抛到进程级，否则日志器反而成了崩溃源。
            }
        }

        private static void AppendLine(string line)
        {
            if (string.IsNullOrEmpty(_logDirectory))
            {
                return;
            }

            try
            {
                var path = ResolveCurrentFilePath();
                File.AppendAllText(path, line + Environment.NewLine, new UTF8Encoding(false));
            }
            catch
            {
                // 磁盘满/文件被占用等情况静默跳过，不能影响采集主流程。
            }
        }
        private static string ResolveCurrentFilePath()
        {
            var today = DateTime.Now.Date;
            if (_currentFilePath != null && _currentFileDate == today)
            {
                var info = new FileInfo(_currentFilePath);
                if (!info.Exists || info.Length < MaxFileBytes)
                {
                    return _currentFilePath;
                }

                _currentFileIndex++;
            }
            else
            {
                _currentFileDate = today;
                _currentFileIndex = 0;
            }

            var stamp = today.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var name = _currentFileIndex == 0
                ? _component + "-" + stamp + ".log"
                : _component + "-" + stamp + "." + _currentFileIndex.ToString(CultureInfo.InvariantCulture) + ".log";

            _currentFilePath = Path.Combine(_logDirectory, name);
            return _currentFilePath;
        }

        /// <summary>
        /// 优先用程序目录下的子目录；若不可写（例如装在 Program Files 下），退回 LocalAppData。
        /// </summary>
        private static string ResolveWritableDirectory(string subFolder)
        {
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", subFolder),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TPEM_ManagementSystem",
                    subFolder)
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    Directory.CreateDirectory(candidate);
                    var probe = Path.Combine(candidate, ".writable");
                    File.WriteAllText(probe, string.Empty);
                    File.Delete(probe);
                    return candidate;
                }
                catch
                {
                }
            }

            return null;
        }
        private static void TryCleanupOldFiles()
        {
            if (string.IsNullOrEmpty(_logDirectory))
            {
                return;
            }

            try
            {
                var deadline = DateTime.Now.Date.AddDays(-MaxRetainedDays);
                foreach (var file in Directory.GetFiles(_logDirectory, _component + "-*.log"))
                {
                    if (File.GetLastWriteTime(file).Date < deadline)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
            }
        }

        private static string LevelTag(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return "DEBUG";
                case LogLevel.Info:
                    return "INFO ";
                case LogLevel.Warn:
                    return "WARN ";
                case LogLevel.Error:
                    return "ERROR";
                default:
                    return "-----";
            }
        }

        private static string SafeUserName()
        {
            try
            {
                return Environment.UserName;
            }
            catch
            {
                return "unknown";
            }
        }

        private static string SafeMachineName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>从 App.config 解析日志级别，缺失或非法时用 Info。</summary>
        public static LogLevel ParseLevel(string value, LogLevel defaultValue)
        {
            LogLevel parsed;
            return Enum.TryParse(value, true, out parsed) ? parsed : defaultValue;
        }
    }
}

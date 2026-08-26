using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tpem.Diagnostics;

namespace MeterAcquisition
{
    internal static class Program
    {
        /// <summary>
        /// 单实例互斥体名。两个实例同时运行会争抢同一个 COM 口，
        /// 并且用同一个 MQTT ClientId 互相顶下线造成重连风暴（P0-7）。
        /// </summary>
        private const string SingleInstanceMutexName = @"Global\TPEM_MeterAcquisition_SingleInstance";

        private static Mutex _singleInstanceMutex;

        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        private static void Main()
        {
            AppLogger.Initialize(
                "acquisition",
                AppLogger.ParseLevel(ConfigurationManager.AppSettings["LogLevel"], Tpem.Diagnostics.LogLevel.Info),
                false);

            InstallGlobalExceptionHandlers();

            try
            {
                if (!TryAcquireSingleInstanceLock())
                {
                    AppLogger.Warn("Startup", "已有一个上位机实例在运行，本次启动被拒绝。");
                    MessageBox.Show(
                        "上位机已经在运行中。\n\n" +
                        "重复启动会争抢同一个串口并造成 MQTT 反复掉线，因此本次启动已取消。",
                        "程序已在运行",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                AppLogger.Info("Startup", "上位机启动。");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                AppLogger.Info("Shutdown", "上位机正常退出。");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Startup", "启动或主循环发生未处理异常，程序退出。", ex);
                ShowFatal(ex);
            }
            finally
            {
                ReleaseSingleInstanceLock();
                AppLogger.Shutdown();
            }
        }

        /// <summary>
        /// 三道异常兜底（P0-1）：
        /// 1. UI 线程（含 async void 处理器抛回同步上下文的异常）
        /// 2. 非 UI 线程的未处理异常
        /// 3. 无人 await 的 Task 异常
        /// 目标是"任何异常都必须留下堆栈"，而不是弹一个没有上下文的默认对话框。
        /// </summary>
        private static void InstallGlobalExceptionHandlers()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (sender, e) =>
            {
                AppLogger.Error("UnhandledUI", "UI 线程未处理异常。", e.Exception);
                ShowRecoverable(e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                AppLogger.Error(
                    "UnhandledDomain",
                    "后台线程未处理异常，IsTerminating=" + e.IsTerminating + "。",
                    ex);
                AppLogger.Shutdown();
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                AppLogger.Error("UnobservedTask", "存在无人处理的 Task 异常。", e.Exception);
                e.SetObserved();
            };
        }

        private static bool TryAcquireSingleInstanceLock()
        {
            try
            {
                bool createdNew;
                _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out createdNew);
                if (createdNew)
                {
                    return true;
                }

                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                return false;
            }
            catch (Exception ex)
            {
                // 互斥体不可用（例如受限账户）时不阻断启动，只记录。
                AppLogger.Warn("Startup", "单实例检查失败，继续启动。", ex);
                return true;
            }
        }

        private static void ReleaseSingleInstanceLock()
        {
            if (_singleInstanceMutex == null)
            {
                return;
            }

            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch
            {
            }

            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        private static void ShowRecoverable(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    "程序遇到一个未处理的错误，已记录到日志，采集会继续运行。\n\n" +
                    ex.Message + "\n\n日志目录: " + (AppLogger.LogDirectory ?? "(不可用)"),
                    "运行时错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch
            {
            }
        }

        private static void ShowFatal(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    "程序发生致命错误，即将退出。详细堆栈已写入日志。\n\n" +
                    ex.Message + "\n\n日志目录: " + (AppLogger.LogDirectory ?? "(不可用)"),
                    "致命错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }
}

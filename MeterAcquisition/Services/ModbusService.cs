using System;
using System.IO.Ports;
using System.Threading;
using Tpem.Diagnostics;


namespace MeterAcquisition
{
    /// <summary>
    /// Modbus RTU 通信服务（底层通信）
    /// </summary>
    public class ModbusService : IDisposable
    {
        private SerialPort _serialPort;
        private readonly object _lockObj = new object();
        private bool _disposed = false;

        // P2-3：通信质量统计。改造前所有失败都退化成 return null，
        // "超时 / CRC 错 / 收到别人的回包 / 设备返回异常码"四种完全不同的问题无法区分，
        // 也就无法回答"这条总线到底健不健康"。
        private long _successCount;
        private long _timeoutCount;
        private long _crcErrorCount;
        private long _mismatchCount;
        private long _exceptionCount;
        private long _errorCount;

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<Exception> CommunicationError;

        public bool IsConnected => _serialPort?.IsOpen ?? false;
        public CommunicationConfig Config { get; private set; }

        public ModbusService()
        {
            Config = new CommunicationConfig();
            InitializeSerialPort();
        }

        private void InitializeSerialPort()
        {
            _serialPort = new SerialPort
            {
                BaudRate = Config.BaudRate,
                Parity = Config.Parity,
                DataBits = Config.DataBits,
                StopBits = Config.StopBits,
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
        }

        /// <summary>
        /// 获取可用串口列表
        /// </summary>
        public string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// 连接串口。
        ///
        /// 注意：串口状态事件**不能在 _lockObj 内触发**。订阅者（MainForm.UpdateConnectionUI）
        /// 会在"已连接"分支里同步发布档案消息，而 MQTT 在 Broker 不可达时默认要等 100 秒才超时 ——
        /// 事件在锁内触发会把串口锁交给 UI 线程整整 100 秒，期间所有轮询都堵在 ReadHoldingRegisters 上，
        /// 表现为界面"未响应"且整段数据缺失。因此先在锁内完成开/关串口，出锁后再通知订阅者。
        /// </summary>
        public bool Connect(string portName)
        {
            bool opened = false;
            Exception error = null;

            lock (_lockObj)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();

                    _serialPort.PortName = portName;
                    _serialPort.Open();

                    AppLogger.Info(
                        "Serial",
                        string.Format(
                            "已打开 {0}: {1},{2},{3},{4}",
                            portName, Config.BaudRate, Config.Parity, Config.DataBits, Config.StopBits));
                    opened = true;
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            }

            if (opened)
            {
                OnConnectionStateChanged(true, portName, "连接成功");
                return true;
            }

            AppLogger.Error("Serial", "打开串口 " + portName + " 失败。", error);
            OnConnectionStateChanged(false, portName, "连接失败: " + (error == null ? "未知原因" : error.Message));
            if (error != null)
            {
                OnCommunicationError(error);
            }

            return false;
        }

        /// <summary>
        /// 断开连接。事件同样在锁外触发，原因见 Connect。
        /// </summary>
        public void Disconnect()
        {
            string portName = null;

            lock (_lockObj)
            {
                if (_serialPort?.IsOpen == true)
                {
                    portName = _serialPort.PortName;
                    _serialPort.Close();
                }
            }

            if (portName != null)
            {
                AppLogger.Info("Serial", "已关闭 " + portName + "。");
                OnConnectionStateChanged(false, portName, "已断开");
            }
        }

        /// <summary>
        /// 更新通讯配置
        /// </summary>
        public void UpdateConfig(CommunicationConfig config)
        {
            lock (_lockObj)
            {
                Config = config;
                if (_serialPort == null)
                {
                    return;
                }

                _serialPort.BaudRate = config.BaudRate;
                _serialPort.Parity = config.Parity;
                _serialPort.DataBits = config.DataBits;
                _serialPort.StopBits = config.StopBits;
            }
        }

        /// <summary>
        /// 读取保持寄存器（P2-3 已加帧校验）。
        ///
        /// 改造前只校验 CRC，不校验从站地址、功能码与字节数：
        /// RS485 是共享总线，多方轮询时收到**别的从站**的回包是常态，
        /// 那种回包 CRC 是合法的，于是会被当成本次请求的数据直接采信 ——
        /// 表现为某台表偶尔出现另一台表的读数，且无从察觉。
        /// 现在按顺序校验：长度 → 从站地址 → 功能码/异常码 → 字节数 → CRC，
        /// 任一不符都判失败并记录具体原因，同时计入通信质量统计。
        /// </summary>
        public byte[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort quantity)
        {
            lock (_lockObj)
            {
                if (!IsConnected)
                {
                    return null;
                }

                try
                {
                    byte[] frame = BuildReadRequestFrame(slaveAddress, startAddress, quantity);
                    _serialPort.DiscardInBuffer();
                    _serialPort.Write(frame, 0, frame.Length);

                    int expectedLength = 5 + quantity * 2;
                    byte[] response = ReadResponse(expectedLength);

                    return ValidateReadResponse(response, slaveAddress, startAddress, quantity, expectedLength);
                }
                catch (TimeoutException)
                {
                    Interlocked.Increment(ref _timeoutCount);
                    return null;
                }
                catch (InvalidOperationException)
                {
                    // 串口已关闭/正在关闭，属于退出时的常见竞态。
                    return null;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _errorCount);
                    AppLogger.Error("Modbus", "读从站 " + slaveAddress + " 寄存器 0x" + startAddress.ToString("X4") + " 时异常。", ex);
                    OnCommunicationError(ex);
                    return null;
                }
            }
        }

        /// <summary>
        /// 逐项校验读响应，通过则返回纯数据区，否则返回 null 并记录原因。
        /// </summary>
        private byte[] ValidateReadResponse(byte[] response, byte slaveAddress, ushort startAddress, ushort quantity, int expectedLength)
        {
            var where = "从站 " + slaveAddress + " 寄存器 0x" + startAddress.ToString("X4");

            if (response == null || response.Length < 3)
            {
                Interlocked.Increment(ref _timeoutCount);
                AppLogger.Debug("Modbus", where + " 无响应或响应过短（" + (response == null ? 0 : response.Length) + " 字节）。");
                return null;
            }

            // 1) 从站地址必须是我们问的那一台，否则是总线上别人的回包。
            if (response[0] != slaveAddress)
            {
                Interlocked.Increment(ref _mismatchCount);
                AppLogger.Warn(
                    "Modbus",
                    where + " 收到的响应来自从站 " + response[0] + "，与请求不符，已丢弃（总线串话或帧未对齐）。");
                return null;
            }

            // 2) 异常响应：功能码最高位置位，第 3 字节是异常码。
            if ((response[1] & 0x80) != 0)
            {
                Interlocked.Increment(ref _exceptionCount);
                AppLogger.Warn(
                    "Modbus",
                    where + " 返回 Modbus 异常码 0x" + response[2].ToString("X2") + "（功能码 0x" + response[1].ToString("X2") + "）。");
                return null;
            }

            // 3) 功能码必须是读保持寄存器。
            if (response[1] != 0x03)
            {
                Interlocked.Increment(ref _mismatchCount);
                AppLogger.Warn("Modbus", where + " 响应功能码为 0x" + response[1].ToString("X2") + "，期望 0x03，已丢弃。");
                return null;
            }

            // 4) 字节数必须等于请求量 × 2，否则解析偏移一定错位。
            int declaredBytes = response[2];
            if (declaredBytes != quantity * 2)
            {
                Interlocked.Increment(ref _mismatchCount);
                AppLogger.Warn(
                    "Modbus",
                    where + " 响应声明 " + declaredBytes + " 字节，期望 " + (quantity * 2) +
                    " 字节，已丢弃（多半是设备型号或协议表与配置不符）。");
                return null;
            }

            if (response.Length < expectedLength)
            {
                Interlocked.Increment(ref _timeoutCount);
                AppLogger.Debug("Modbus", where + " 响应不完整：实收 " + response.Length + "，需要 " + expectedLength + "。");
                return null;
            }

            // 5) CRC 只对"这一帧应有的长度"计算，多余字节不参与，
            //    避免上一次超时残留的尾字节把本次好帧判成坏帧。
            if (!Crc16Helper.Validate(response, expectedLength))
            {
                Interlocked.Increment(ref _crcErrorCount);
                AppLogger.Warn("Modbus", where + " CRC 校验失败，已丢弃（线路干扰或帧未对齐）。");
                return null;
            }

            var data = new byte[declaredBytes];
            Array.Copy(response, 3, data, 0, declaredBytes);
            Interlocked.Increment(ref _successCount);
            return data;
        }

        /// <summary>通信质量统计（P2-3）。改造前失败原因全部退化成 null，无法区分也无法统计。</summary>
        public string GetStatisticsSummary()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "成功 {0}，超时/不完整 {1}，CRC 错 {2}，身份/长度不符 {3}，设备异常码 {4}，其他异常 {5}",
                Interlocked.Read(ref _successCount),
                Interlocked.Read(ref _timeoutCount),
                Interlocked.Read(ref _crcErrorCount),
                Interlocked.Read(ref _mismatchCount),
                Interlocked.Read(ref _exceptionCount),
                Interlocked.Read(ref _errorCount));
        }

        public ushort[] ReadHoldingRegisterValues(byte slaveAddress, ushort startAddress, ushort quantity)
        {
            byte[] data = ReadHoldingRegisters(slaveAddress, startAddress, quantity);
            if (data == null || data.Length != quantity * 2)
                return null;

            var values = new ushort[quantity];
            for (int index = 0; index < quantity; index++)
                values[index] = (ushort)((data[index * 2] << 8) | data[index * 2 + 1]);

            return values;
        }

        /// <summary>
        /// 写入多个保持寄存器
        /// </summary>
        public bool WriteMultipleRegisters(byte slaveAddress, ushort startAddress, byte[] data)
        {
            lock (_lockObj)
            {
                if (!IsConnected)
                    return false;

                try
                {
                    byte[] frame = BuildWriteMultipleRequestFrame(slaveAddress, startAddress, data);
                    _serialPort.DiscardInBuffer();
                    _serialPort.Write(frame, 0, frame.Length);

                    byte[] response = ReadResponse(8);

                    return ValidateWriteResponse(
                        response,
                        slaveAddress,
                        0x10,
                        startAddress,
                        (ushort)(data.Length / 2),
                        "写从站 " + slaveAddress + " 寄存器 0x" + startAddress.ToString("X4"));
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    OnCommunicationError(ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 写入单个线圈（遥控操作）
        /// </summary>
        public bool WriteSingleCoil(byte slaveAddress, ushort address, bool value)
        {
            lock (_lockObj)
            {
                if (!IsConnected)
                    return false;

                try
                {
                    byte[] frame = BuildWriteCoilRequestFrame(slaveAddress, address, value);
                    _serialPort.DiscardInBuffer();
                    _serialPort.Write(frame, 0, frame.Length);

                    byte[] response = ReadResponse(8);

                    return ValidateWriteResponse(
                        response,
                        slaveAddress,
                        0x05,
                        address,
                        value ? (ushort)0xFF00 : (ushort)0x0000,
                        "遥控从站 " + slaveAddress + " 线圈 0x" + address.ToString("X4"));
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (Exception ex)
                {
                    OnCommunicationError(ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 逐项校验写响应。
        ///
        /// 改造前只判 CRC 与功能码，且 CRC 按"实收长度"计算，两个方向都会出错：
        ///  · 不校验从站地址 —— RS485 共享总线上收到别的从站的回包时会被当成本次写入成功，
        ///    对 DO1/DO2 分合闸就是"操作者以为断路器已经动作了"；
        ///  · 实收超过 8 字节（廉价 USB-RS485 的 TX 回显、上次超时残留的尾字节）时，
        ///    CRC 覆盖了多余的字节必然失败 —— 明明写成功却报"设备未确认"，
        ///    而界面提示"命令可能已到达…请重试"，会诱导操作者重复下发分/合闸。
        ///
        /// 现在在缓冲区里按 8 字节定长找一帧：从站地址、功能码、地址与写入值回显、CRC 全部相符才算成功，
        /// 因此既不会采信别人的回包，也不会因为多读了几个字节就把好帧判成坏帧。
        /// </summary>
        private bool ValidateWriteResponse(byte[] response, byte slaveAddress, byte functionCode, ushort address, ushort value, string where)
        {
            if (response == null || response.Length < 8)
            {
                Interlocked.Increment(ref _timeoutCount);
                AppLogger.Debug("Modbus", where + " 无写响应或响应过短（" + (response == null ? 0 : response.Length) + " 字节）。");
                return false;
            }

            bool sawForeignFrame = false;

            for (int offset = 0; offset + 8 <= response.Length; offset++)
            {
                if (response[offset] != slaveAddress)
                {
                    sawForeignFrame = true;
                    continue;
                }

                // 异常响应：功能码最高位置位，第 3 字节是异常码。
                if ((response[offset + 1] & 0x80) != 0)
                {
                    Interlocked.Increment(ref _exceptionCount);
                    AppLogger.Warn(
                        "Modbus",
                        where + " 返回 Modbus 异常码 0x" + response[offset + 2].ToString("X2") +
                        "（功能码 0x" + response[offset + 1].ToString("X2") + "），本次写入未生效。");
                    return false;
                }

                if (response[offset + 1] != functionCode)
                {
                    sawForeignFrame = true;
                    continue;
                }

                if (!Crc16Helper.Validate(response, offset, 8))
                {
                    continue;
                }

                ushort echoedAddress = (ushort)((response[offset + 2] << 8) | response[offset + 3]);
                ushort echoedValue = (ushort)((response[offset + 4] << 8) | response[offset + 5]);
                if (echoedAddress != address || echoedValue != value)
                {
                    Interlocked.Increment(ref _mismatchCount);
                    AppLogger.Warn(
                        "Modbus",
                        where + " 写响应回显不符（地址 0x" + echoedAddress.ToString("X4") +
                        "、值 0x" + echoedValue.ToString("X4") + "），已丢弃。");
                    return false;
                }

                Interlocked.Increment(ref _successCount);
                return true;
            }

            if (sawForeignFrame)
            {
                Interlocked.Increment(ref _mismatchCount);
                AppLogger.Warn("Modbus", where + " 缓冲区里没有本从站的合法写响应，已丢弃（总线串话或帧未对齐）。");
            }
            else
            {
                Interlocked.Increment(ref _crcErrorCount);
                AppLogger.Warn("Modbus", where + " 写响应 CRC 校验失败，已丢弃（线路干扰或帧未对齐）。");
            }

            return false;
        }

        /// <summary>
        /// 探测从站地址
        /// </summary>
        public byte? DiscoverSlaveAddress()
        {
            // 优先尝试默认地址
            byte[] priorityList = { 100, 1, 2, 3 };

            foreach (byte addr in priorityList)
            {
                if (TestSlaveAddress(addr))
                    return addr;
            }

            // 全范围扫描
            for (byte addr = 1; addr <= 247; addr++)
            {
                if (System.Array.IndexOf(priorityList, addr) >= 0)
                    continue;

                if (TestSlaveAddress(addr))
                    return addr;
            }

            return null;
        }

        private bool TestSlaveAddress(byte address)
        {
            byte[] response = ReadHoldingRegisters(address, 0x9800, 10);
            return response != null && response.Length >= 20;
        }

        #region 帧构建方法

        private byte[] BuildReadRequestFrame(byte slaveAddress, ushort startAddress, ushort quantity)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveAddress;
            frame[1] = 0x03;
            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);
            frame[4] = (byte)(quantity >> 8);
            frame[5] = (byte)(quantity & 0xFF);

            ushort crc = Crc16Helper.Calculate(frame, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);

            return frame;
        }

        private byte[] BuildWriteMultipleRequestFrame(byte slaveAddress, ushort startAddress, byte[] data)
        {
            int dataLength = data.Length;
            byte[] frame = new byte[9 + dataLength];
            frame[0] = slaveAddress;
            frame[1] = 0x10;
            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);
            frame[4] = (byte)((dataLength / 2) >> 8);
            frame[5] = (byte)((dataLength / 2) & 0xFF);
            frame[6] = (byte)dataLength;
            Array.Copy(data, 0, frame, 7, dataLength);

            ushort crc = Crc16Helper.Calculate(frame, 7 + dataLength);
            frame[7 + dataLength] = (byte)(crc & 0xFF);
            frame[8 + dataLength] = (byte)(crc >> 8);

            return frame;
        }

        private byte[] BuildWriteCoilRequestFrame(byte slaveAddress, ushort address, bool value)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveAddress;
            frame[1] = 0x05;
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);
            frame[4] = value ? (byte)0xFF : (byte)0x00;
            frame[5] = 0x00;

            ushort crc = Crc16Helper.Calculate(frame, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);

            return frame;
        }

        #endregion

        private byte[] ReadResponse(int expectedLength)
        {
            byte[] buffer = new byte[expectedLength + 10];
            int bytesRead = 0;
            DateTime timeout = DateTime.Now.AddMilliseconds(500);

            while (DateTime.Now < timeout && bytesRead < expectedLength)
            {
                if (_serialPort.BytesToRead > 0)
                {
                    int toRead = Math.Min(_serialPort.BytesToRead, buffer.Length - bytesRead);
                    bytesRead += _serialPort.Read(buffer, bytesRead, toRead);
                }
                Thread.Sleep(10);
            }

            if (bytesRead > 0)
            {
                byte[] result = new byte[bytesRead];
                Array.Copy(buffer, result, bytesRead);
                return result;
            }

            return null;
        }

        protected virtual void OnConnectionStateChanged(bool isConnected, string portName, string message)
        {
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                IsConnected = isConnected,
                PortName = portName,
                Message = message
            });
        }

        protected virtual void OnCommunicationError(Exception ex)
        {
            CommunicationError?.Invoke(this, ex);
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                // 先关闭再释放：直接 Dispose 一个仍打开的 SerialPort 在部分 USB 转串口驱动上会抛异常。
                try
                {
                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("Serial", "关闭串口时发生异常。", ex);
                }

                try
                {
                    _serialPort?.Dispose();
                }
                catch (Exception ex)
                {
                    AppLogger.Warn("Serial", "释放串口时发生异常。", ex);
                }

                _serialPort = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
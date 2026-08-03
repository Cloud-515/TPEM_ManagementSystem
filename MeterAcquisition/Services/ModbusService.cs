using System;
using System.IO.Ports;
using System.Threading;


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
        /// 连接串口
        /// </summary>
        public bool Connect(string portName)
        {
            lock (_lockObj)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();

                    _serialPort.PortName = portName;
                    _serialPort.Open();

                    OnConnectionStateChanged(true, portName, "连接成功");
                    return true;
                }
                catch (Exception ex)
                {
                    OnConnectionStateChanged(false, portName, $"连接失败: {ex.Message}");
                    OnCommunicationError(ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            lock (_lockObj)
            {
                if (_serialPort?.IsOpen == true)
                {
                    string portName = _serialPort.PortName;
                    _serialPort.Close();
                    OnConnectionStateChanged(false, portName, "已断开");
                }
            }
        }

        /// <summary>
        /// 更新通讯配置
        /// </summary>
        public void UpdateConfig(CommunicationConfig config)
        {
            Config = config;
            _serialPort.BaudRate = config.BaudRate;
            _serialPort.Parity = config.Parity;
            _serialPort.DataBits = config.DataBits;
            _serialPort.StopBits = config.StopBits;
        }

        /// <summary>
        /// 读取保持寄存器
        /// </summary>
        public byte[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort quantity)
        {
            lock (_lockObj)
            {
                if (!IsConnected)
                    return null;

                try
                {
                    byte[] frame = BuildReadRequestFrame(slaveAddress, startAddress, quantity);
                    _serialPort.DiscardInBuffer();
                    _serialPort.Write(frame, 0, frame.Length);

                    int expectedLength = 5 + quantity * 2;
                    byte[] response = ReadResponse(expectedLength);

                    if (response != null && Crc16Helper.Validate(response, response.Length))
                    {
                        if (response[1] == 0x03)
                        {
                            byte[] data = new byte[response[2]];
                            Array.Copy(response, 3, data, 0, data.Length);
                            return data;
                        }
                        else if ((response[1] & 0x80) != 0)
                        {
                            throw new Exception($"Modbus 异常码: 0x{response[2]:X2}");
                        }
                    }

                    return null;
                }
                catch (TimeoutException)
                {
                    return null;
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    OnCommunicationError(ex);
                    return null;
                }
            }
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

                    if (response != null && Crc16Helper.Validate(response, response.Length))
                    {
                        return response[1] == 0x10;
                    }

                    return false;
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

                    if (response != null && Crc16Helper.Validate(response, response.Length))
                    {
                        return response[1] == 0x05;
                    }

                    return false;
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
            if (!_disposed)
            {
                _serialPort?.Dispose();
                _disposed = true;
            }
        }
    }
}
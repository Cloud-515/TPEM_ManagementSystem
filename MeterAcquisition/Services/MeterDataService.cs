

using System;
using System.IO.Ports;
using System.Text;

namespace MeterAcquisition
{
    /// <summary>
    /// 电表数据业务逻辑服务
    /// </summary>
    public class MeterDataService
    {
        private readonly ModbusService _modbusService;

        public MeterDataService(ModbusService modbusService) { _modbusService = modbusService; }

        public byte SlaveAddress => _modbusService.Config.SlaveAddress;

        #region 实时数据读取

        /// <summary>
        /// 读取实时测量数据
        /// </summary>
        public RealTimeData ReadRealTimeData() => ReadRealTimeData(SlaveAddress);

        public RealTimeData ReadRealTimeData(byte slaveAddress)
        {
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, 0x1581, 58);
            //byte[] response = _modbusService.ReadHoldingRegisters(SlaveAddress, 0x15B9, 2);
            if((response == null) || (response.Length < 112))
            {
                return null;
            }
            RealTimeData data = new RealTimeData();
            int offset = 0;
            data.VoltageA = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            data.VoltageB = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            data.VoltageC = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            // 跳过平均相电压
            offset += 4;
            data.VoltageAB = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            data.VoltageBC = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            data.VoltageCA = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            // 跳过平均线电压
            offset += 4;
            data.CurrentA = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            data.CurrentB = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            data.CurrentC = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            // 跳过平均电流
            offset += 4;
            // 跳过各相有功功率
            //offset += 16;
            offset += 12;
            data.ActivePowerTotal = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            // 跳过各相无功功率
            offset += 12;
            data.ReactivePowerTotal = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            // 跳过各相视在功率
            offset += 12;
            data.ApparentPowerTotal = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            // 跳过各相功率因数
            offset += 12;
            data.PowerFactorTotal = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            offset += 4;
            data.Frequency = BitConverter.ToSingle(GetReversedBytes(response, offset), 0);
            return data;
        }

        /// <summary>
        /// 读取电能数据
        /// </summary>
        public EnergyData ReadEnergyData() => ReadEnergyData(SlaveAddress);

        public EnergyData ReadEnergyData(byte slaveAddress)
        {
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, 0x01F4, 12);
            if((response == null) || (response.Length < 16))
            {
                return null;
            }
            EnergyData data = new EnergyData();
            int offset = 0;
            data.ForwardActiveEnergy = BitConverter.ToInt32(GetReversedBytes(response, offset), 0) / 100.0f;
            offset += 4;
            data.ReverseActiveEnergy = BitConverter.ToInt32(GetReversedBytes(response, offset), 0) / 100.0f;
            offset += 4;
            offset += 4;
            // 跳过预留
            offset += 4;
            data.ForwardReactiveEnergy = BitConverter.ToInt32(GetReversedBytes(response, offset), 0) / 100.0f;
            offset += 4;
            // todo
            data.ReverseReactiveEnergy = BitConverter.ToInt32(GetReversedBytes(response, offset), 0) / 100.0f;
            return data;
        }

        /// <summary>
        /// 读取电能质量数据
        /// </summary>
        public PowerQualityData ReadPowerQualityData() => ReadPowerQualityData(SlaveAddress);

        public PowerQualityData ReadPowerQualityData(byte slaveAddress)
        {
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, 0x0514, 58);
            if((response == null) || (response.Length < 40))
            {
                return null;
            }
            PowerQualityData data = new PowerQualityData();
            // 跳过畸变值
            int offset = 12;
            data.CurrentTHDA = BitConverter.ToSingle(GetReversedBytes(response, offset), 0) * 100;
            offset += 4;
            data.CurrentTHDB = BitConverter.ToSingle(GetReversedBytes(response, offset), 0) * 100;
            offset += 4;
            data.CurrentTHDC = BitConverter.ToSingle(GetReversedBytes(response, offset), 0) * 100;
            offset += 4;
            // 跳过预留
            offset += 24;
            // 跳过畸变值
            offset += 12;
            data.VoltageTHDA = BitConverter.ToSingle(GetReversedBytes(response, offset), 0) * 100;
            offset += 4;
            data.VoltageTHDB = BitConverter.ToSingle(GetReversedBytes(response, offset), 0) * 100;
            offset += 4;
            data.VoltageTHDC = BitConverter.ToSingle(GetReversedBytes(response, offset), 0) * 100;
            // 跳过预留
            offset += 40;
            data.VoltageUnbalance = BitConverter.ToSingle(GetReversedBytes(response, offset), 0) * 100;
            offset += 4;
            data.CurrentUnbalance = BitConverter.ToSingle(GetReversedBytes(response, offset), 0) * 100;
            return data;
        }
        #endregion

        #region 参数读取与设置

        /// <summary>
        /// 读取通讯参数
        /// </summary>
        public CommunicationConfig ReadCommunicationConfig()
        {
            byte[] response = _modbusService.ReadHoldingRegisters(SlaveAddress, 0x1901, 3);
            if((response == null) || (response.Length < 6))
            {
                return null;
            }
            CommunicationConfig config = new CommunicationConfig
            {
                SlaveAddress = (byte)((response[0] << 8) | response[1]),
                BaudRate = ParseBaudRate((response[2] << 8) | response[3]),
                Parity = ParseParity((response[4] << 8) | response[5])
            };
            return config;
        }

        /// <summary>
        /// 写入通讯参数
        /// </summary>
        public bool WriteCommunicationConfig(CommunicationConfig config)
        {
            byte[] data = new byte[6];
            data[0] = (byte)(config.SlaveAddress >> 8);
            data[1] = (byte)(config.SlaveAddress & 0xFF);
            data[2] = (byte)(GetBaudRateIndex(config.BaudRate) >> 8);
            data[3] = (byte)(GetBaudRateIndex(config.BaudRate) & 0xFF);
            data[4] = (byte)(GetParityIndex(config.Parity) >> 8);
            data[5] = (byte)(GetParityIndex(config.Parity) & 0xFF);
            return _modbusService.WriteMultipleRegisters(SlaveAddress, 0x6401, data);
        }

        /// <summary>
        /// 读取设备信息
        /// </summary>
        public DeviceInfo ReadDeviceInfo() => ReadDeviceInfo(SlaveAddress);

        public DeviceInfo ReadDeviceInfo(byte slaveAddress)
        {
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, 0x2648, 30);
            if((response == null) || (response.Length < 60))
            {
                return null;
            }
            DeviceInfo info = new DeviceInfo();
            // 读取设备类型（20个寄存器，40字节）
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < 40; i += 2)
            {
                char c = (char)((response[i] << 8) | response[i + 1]);
                if((c != 0x20) && (c != 0))
                {
                    sb.Append(c);
                }
            }
            info.DeviceType = sb.ToString().Trim();
            int offset = 40;
            info.ProgramVersion = (ushort)((response[offset] << 8) | response[offset + 1]);
            offset += 2;
            info.ProtocolVersion = (ushort)((response[offset] << 8) | response[offset + 1]);
            offset += 2;
            ushort year = (ushort)((response[offset] << 8) | response[offset + 1]);
            offset += 2;
            ushort month = (ushort)((response[offset] << 8) | response[offset + 1]);
            offset += 2;
            ushort day = (ushort)((response[offset] << 8) | response[offset + 1]);
            offset += 2;
            info.VersionDate = $"{2000 + year:D4}-{month:D2}-{day:D2}";
            info.SerialNumber = (uint)((response[offset] << 24) |
                (response[offset + 1] << 16) |
                (response[offset + 2] << 8) |
                response[offset + 3]);
            offset += 8;
            info.IoConfig = (ushort)((response[offset] << 8) | response[offset + 1]);
            return info;
        }
        #endregion

        #region 控制操作

        /// <summary>
        /// DO1 合闸
        /// </summary>
        public bool DO1_On() { return _modbusService.WriteSingleCoil(SlaveAddress, 0x238C, true); }

        /// <summary>
        /// DO1 分闸
        /// </summary>
        public bool DO1_Off() { return _modbusService.WriteSingleCoil(SlaveAddress, 0x238D, true); }

        /// <summary>
        /// DO2 合闸
        /// </summary>
        public bool DO2_On() { return _modbusService.WriteSingleCoil(SlaveAddress, 0x238E, true); }

        /// <summary>
        /// DO2 分闸
        /// </summary>
        public bool DO2_Off() { return _modbusService.WriteSingleCoil(SlaveAddress, 0x238F, true); }

        /// <summary>
        /// 清除总电能
        /// </summary>
        public bool ClearEnergy()
        {
            byte[] data = new byte[] { 0xFF, 0x00 };
            return _modbusService.WriteMultipleRegisters(SlaveAddress, 0x9601, data);
        }

        /// <summary>
        /// 清除事件记录
        /// </summary>
        public bool ClearEvents()
        {
            byte[] data = new byte[] { 0xFF, 0x00 };
            return _modbusService.WriteMultipleRegisters(SlaveAddress, 0x9609, data);
        }
        #endregion

        #region 辅助方法
        //private byte[] GetReversedBytes(byte[] source, int offset)
        //{
        //    byte[] temp = new byte[] { source[offset + 1], source[offset], source[offset + 3], source[offset + 2] };
        //    return temp;
        //}

        private byte[] GetReversedBytes(byte[] source, int offset)

        {
            byte[] temp = new byte[] { source[offset + 3], source[offset + 2], source[offset + 1], source[offset], };
            return temp;
        }

        private int ParseBaudRate(int index)
        {
            int[] baudRates = { 1200, 2400, 4800, 9600, 19200, 38400 };
            return ((index >= 0) && (index < baudRates.Length)) ? baudRates[index] : 9600;
        }

        private int GetBaudRateIndex(int baudRate)
        {
            int[] baudRates = { 1200, 2400, 4800, 9600, 19200, 38400 };
            return Array.IndexOf(baudRates, baudRate);
        }

        private Parity ParseParity(int index)
        {
            switch(index)
            {
                case 0:
                    return Parity.None; // 8N2
                case 1:
                    return Parity.Odd;  // 8O1
                case 2:
                    return Parity.Even; // 8E1
                case 3:
                    return Parity.None; // 8N1
                case 4:
                    return Parity.Odd;  // 8O2
                case 5:
                    return Parity.Even; // 8E2
                default:
                    return Parity.Even;
            }
        }

        private int GetParityIndex(Parity parity)
        {
            // 默认返回 2 (8E1)
            return 2;
        }
        #endregion
    }
}
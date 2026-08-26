

using System;
using System.IO.Ports;
using System.Text;
using Tpem.Diagnostics;

namespace MeterAcquisition
{
    /// <summary>
    /// 电表数据业务逻辑服务
    /// </summary>
    public class MeterDataService : IMeterDataReader
    {
        // 各读取方法"实际消费到的最大字节数"。
        // 原代码的长度守卫都小于这个值（112/16/40），响应偏短时会在解析途中越界，
        // 且异常被 MeterManager.PollAll 的空 catch 吞掉（主串口路径则直接把程序带崩）。
        // 对应改进项：P0-2。这些常量必须与下面的 offset 推进严格一致。
        private const int RealTimeRequiredBytes = 116;   // 58 寄存器，最后读 Frequency @112..115
        private const int EnergyRequiredBytes = 24;      // 12 寄存器，最后读反向无功 @20..23
        private const int PowerQualityRequiredBytes = 116; // 58 寄存器，最后读电流不平衡 @112..115
        private const int DeviceInfoRequiredBytes = 60;  // 30 寄存器，最后读 IoConfig @58..59
        private const int CommConfigRequiredBytes = 6;   // 3 寄存器

        private readonly ModbusService _modbusService;

        public MeterDataService(ModbusService modbusService) { _modbusService = modbusService; }

        public byte SlaveAddress => _modbusService.Config.SlaveAddress;

        public string DeviceModel => "Legacy";

        public bool Probe(byte slaveAddress)
        {
            return ReadRealTimeData(slaveAddress) != null;
        }

        #region 实时数据读取

        /// <summary>
        /// 读取实时测量数据
        /// </summary>
        public RealTimeData ReadRealTimeData() => ReadRealTimeData(SlaveAddress);

        public RealTimeData ReadRealTimeData(byte slaveAddress)
        {
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, 0x1581, 58);
            //byte[] response = _modbusService.ReadHoldingRegisters(SlaveAddress, 0x15B9, 2);
            if (!IsResponseUsable(response, RealTimeRequiredBytes, slaveAddress, "实时数据", 0x1581))
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
            if (!IsResponseUsable(response, EnergyRequiredBytes, slaveAddress, "电能数据", 0x01F4))
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
            if (!IsResponseUsable(response, PowerQualityRequiredBytes, slaveAddress, "电能质量", 0x0514))
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
            // TODO(P2-1): 此处缺少与其他字段一致的 offset += 4，下面的 "+= 40" 是从 VoltageTHDC
            // 自身偏移起算的，等效于只跳过 36 字节预留区。需对照协议地址表核对；
            // 未经核对不擅自改动，以免静默改变已入库数据的语义。
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
            if (!IsResponseUsable(response, CommConfigRequiredBytes, SlaveAddress, "通讯参数", 0x1901))
            {
                return null;
            }
            CommunicationConfig config = new CommunicationConfig
            {
                SlaveAddress = (byte)((response[0] << 8) | response[1]),
                BaudRate = ParseBaudRate((response[2] << 8) | response[3]),
                Parity = ParseParity((response[4] << 8) | response[5]),
                StopBits = ParseStopBits((response[4] << 8) | response[5])
            };
            return config;
        }

        /// <summary>
        /// 写入通讯参数（P0-3）。
        ///
        /// 原实现有两个严重缺陷：
        /// 1. GetParityIndex 无条件返回 2，用户在界面选择的校验位被完全丢弃，永远写成 8E1；
        /// 2. GetBaudRateIndex 未命中时返回 -1，被截断成 0xFFFF 写进波特率寄存器。
        /// 两者都会让电表从总线上消失，只能到现场逐台恢复。
        /// 现在改为：任何一项无法映射就拒绝下发，并通过 error 说明原因。
        /// </summary>
        public bool WriteCommunicationConfig(CommunicationConfig config, out string error)
        {
            error = null;
            if (config == null)
            {
                error = "通讯参数为空。";
                return false;
            }

            if (config.SlaveAddress < 1 || config.SlaveAddress > 247)
            {
                error = "从站地址必须在 1~247 之间，当前为 " + config.SlaveAddress + "。";
                return false;
            }

            int baudIndex = GetBaudRateIndex(config.BaudRate);
            if (baudIndex < 0)
            {
                error = "设备不支持波特率 " + config.BaudRate + "，可选值: " + string.Join("/", BaudRateTableText()) + "。";
                return false;
            }

            int parityIndex = GetParityIndex(config.Parity, config.StopBits);
            if (parityIndex < 0)
            {
                error = "设备不支持校验位/停止位组合 " + config.Parity + " + " + config.StopBits + "。";
                return false;
            }

            byte[] data = new byte[6];
            data[0] = (byte)(config.SlaveAddress >> 8);
            data[1] = (byte)(config.SlaveAddress & 0xFF);
            data[2] = (byte)(baudIndex >> 8);
            data[3] = (byte)(baudIndex & 0xFF);
            data[4] = (byte)(parityIndex >> 8);
            data[5] = (byte)(parityIndex & 0xFF);

            AppLogger.Info(
                "MeterConfig",
                string.Format(
                    "准备向从站 {0} 写入通讯参数: 地址={1}, 波特率={2}(idx {3}), 校验={4}+{5}(idx {6})。",
                    SlaveAddress, config.SlaveAddress, config.BaudRate, baudIndex,
                    config.Parity, config.StopBits, parityIndex));

            bool ok = _modbusService.WriteMultipleRegisters(SlaveAddress, 0x6401, data);
            if (!ok)
            {
                error = "设备未确认写入（无响应或返回异常）。";
            }

            return ok;
        }

        /// <summary>
        /// 读取设备信息
        /// </summary>
        public DeviceInfo ReadDeviceInfo() => ReadDeviceInfo(SlaveAddress);

        public DeviceInfo ReadDeviceInfo(byte slaveAddress)
        {
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, 0x2648, 30);
            if (!IsResponseUsable(response, DeviceInfoRequiredBytes, slaveAddress, "设备信息", 0x2648))
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

        /// <summary>
        /// 统一的响应可用性检查（P0-2）。
        /// 长度不足时记 Warn 而不是静默返回 null —— 原来这类失败在日志里完全看不见。
        /// </summary>
        private static bool IsResponseUsable(byte[] response, int requiredBytes, byte slaveAddress, string what, ushort startAddress)
        {
            if (response == null)
            {
                // 无响应属于常见的现场情况（掉线/超时），用 Debug 级别避免刷屏，
                // 连续失败的统计由 MeterManager 负责。
                AppLogger.Debug(
                    "Modbus",
                    string.Format("从站 {0} 读取{1}(0x{2:X4}) 无响应。", slaveAddress, what, startAddress));
                return false;
            }

            if (response.Length < requiredBytes)
            {
                AppLogger.Warn(
                    "Modbus",
                    string.Format(
                        "从站 {0} 读取{1}(0x{2:X4}) 响应长度不足：实收 {3} 字节，解析需要 {4} 字节，本次数据丢弃。" +
                        "请核对设备型号与协议地址表是否匹配。",
                        slaveAddress, what, startAddress, response.Length, requiredBytes));
                return false;
            }

            return true;
        }

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

        private static readonly int[] SupportedBaudRates = { 1200, 2400, 4800, 9600, 19200, 38400 };

        private static string[] BaudRateTableText()
        {
            var text = new string[SupportedBaudRates.Length];
            for (int i = 0; i < SupportedBaudRates.Length; i++)
            {
                text[i] = SupportedBaudRates[i].ToString();
            }

            return text;
        }

        private int ParseBaudRate(int index)
        {
            return ((index >= 0) && (index < SupportedBaudRates.Length)) ? SupportedBaudRates[index] : 9600;
        }

        private int GetBaudRateIndex(int baudRate)
        {
            // 未命中返回 -1，由调用方拒绝下发；绝不能让 -1 流到寄存器里（P0-3）。
            return Array.IndexOf(SupportedBaudRates, baudRate);
        }

        // 设备的"校验位"寄存器同时编码了校验方式和停止位：
        // 0=8N2  1=8O1  2=8E1  3=8N1  4=8O2  5=8E2
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

        private StopBits ParseStopBits(int index)
        {
            switch (index)
            {
                case 0: // 8N2
                case 4: // 8O2
                case 5: // 8E2
                    return StopBits.Two;
                case 1: // 8O1
                case 2: // 8E1
                case 3: // 8N1
                    return StopBits.One;
                default:
                    return StopBits.One;
            }
        }

        /// <summary>
        /// ParseParity/ParseStopBits 的逆映射。因为寄存器同时编码校验位与停止位，
        /// 必须两者一起才能确定索引。无法映射时返回 -1（P0-3）。
        /// </summary>
        private int GetParityIndex(Parity parity, StopBits stopBits)
        {
            bool two = stopBits == StopBits.Two;
            switch (parity)
            {
                case Parity.None:
                    return two ? 0 : 3;
                case Parity.Odd:
                    return two ? 4 : 1;
                case Parity.Even:
                    return two ? 5 : 2;
                default:
                    return -1;
            }
        }
        #endregion
    }
}
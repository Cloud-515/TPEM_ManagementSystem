

using System;
using System.IO.Ports;
using System.Text;
using Tpem.Diagnostics;
using Tpem.Protocol;

namespace MeterAcquisition
{
    /// <summary>
    /// 电表数据业务逻辑服务
    /// </summary>
    public class MeterDataService : IMeterDataReader
    {
        private const ushort RealtimeStartAddress = 0x1581;
        private const ushort EnergyStartAddress = 0x01F4;
        private const ushort QualityStartAddress = 0x0514;

        // 各读取方法"实际消费到的最大字节数"由映射表自动推导（P0-2 + P2-1）。
        // 原代码把长度守卫写成 112/16/40，都小于实际消费量，响应偏短时会在解析途中越界；
        // 现在守卫值来自 RegisterMapReader.RequiredLength(map)，不可能再与解析脱节。
        private const int DeviceInfoRequiredBytes = 60;  // 30 寄存器，最后读 IoConfig @58..59
        private const int CommConfigRequiredBytes = 6;   // 3 寄存器

        /// <summary>
        /// 原有电表实时数据映射（P2-1）。
        /// 偏移直接写出，取代原来一长串 `offset += 4` / `// 跳过...` 的累加 ——
        /// 那种写法下"某个字段到底落在第几字节"必须靠人从头数一遍。
        /// 该表 3 个平均值字段与各相分量按协议是跳过的，未列入。
        /// </summary>
        private static readonly RegisterField<RealTimeData>[] RealtimeMap =
        {
            new RegisterField<RealTimeData>("VoltageA", 0, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageA = v),
            new RegisterField<RealTimeData>("VoltageB", 4, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageB = v),
            new RegisterField<RealTimeData>("VoltageC", 8, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageC = v),
            // 12: 平均相电压（跳过）
            new RegisterField<RealTimeData>("VoltageAB", 16, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageAB = v),
            new RegisterField<RealTimeData>("VoltageBC", 20, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageBC = v),
            new RegisterField<RealTimeData>("VoltageCA", 24, RegisterValueType.Float32, 1f, "V", (d, v) => d.VoltageCA = v),
            // 28: 平均线电压（跳过）
            new RegisterField<RealTimeData>("CurrentA", 32, RegisterValueType.Float32, 1f, "A", (d, v) => d.CurrentA = v),
            new RegisterField<RealTimeData>("CurrentB", 36, RegisterValueType.Float32, 1f, "A", (d, v) => d.CurrentB = v),
            new RegisterField<RealTimeData>("CurrentC", 40, RegisterValueType.Float32, 1f, "A", (d, v) => d.CurrentC = v),
            // 44: 平均电流（跳过）；48~59: 各相有功功率（跳过）
            new RegisterField<RealTimeData>("ActivePowerTotal", 60, RegisterValueType.Float32, 1f, "W", (d, v) => d.ActivePowerTotal = v),
            // 64~75: 各相无功功率（跳过）
            new RegisterField<RealTimeData>("ReactivePowerTotal", 76, RegisterValueType.Float32, 1f, "var", (d, v) => d.ReactivePowerTotal = v),
            // 80~91: 各相视在功率（跳过）
            new RegisterField<RealTimeData>("ApparentPowerTotal", 92, RegisterValueType.Float32, 1f, "VA", (d, v) => d.ApparentPowerTotal = v),
            // 96~107: 各相功率因数（跳过）
            new RegisterField<RealTimeData>("PowerFactorTotal", 108, RegisterValueType.Float32, 1f, string.Empty, (d, v) => d.PowerFactorTotal = v),
            new RegisterField<RealTimeData>("Frequency", 112, RegisterValueType.Float32, 1f, "Hz", (d, v) => d.Frequency = v)
        };

        /// <summary>原有电表电能映射。原始为 32 位整数，0.01 一档，故 Scale=0.01。</summary>
        private static readonly RegisterField<EnergyData>[] EnergyMap =
        {
            new RegisterField<EnergyData>("ForwardActiveEnergy", 0, RegisterValueType.Int32, 0.01f, "kWh", (d, v) => d.ForwardActiveEnergy = v),
            new RegisterField<EnergyData>("ReverseActiveEnergy", 4, RegisterValueType.Int32, 0.01f, "kWh", (d, v) => d.ReverseActiveEnergy = v),
            // 8~15: 预留（跳过）
            new RegisterField<EnergyData>("ForwardReactiveEnergy", 16, RegisterValueType.Int32, 0.01f, "kvarh", (d, v) => d.ForwardReactiveEnergy = v),
            new RegisterField<EnergyData>("ReverseReactiveEnergy", 20, RegisterValueType.Int32, 0.01f, "kvarh", (d, v) => d.ReverseReactiveEnergy = v)
        };

        /// <summary>
        /// 原有电表电能质量映射。原始为比率浮点，×100 换成百分比。
        ///
        /// TODO(P2-1)：VoltageUnbalance / CurrentUnbalance 的偏移 108 / 112 沿用改造前的推导结果
        /// （原代码在读完 VoltageTHDC 后缺少一次 `offset += 4`，随后 `offset += 40`，等效于只跳过 36 字节预留区）。
        /// 现在偏移是显式的，这个可疑点从"藏在算术里"变成"写在表上"，但仍需对照协议地址表核对；
        /// 未经核对不改动，以免静默改变已入库数据的语义。
        /// </summary>
        private static readonly RegisterField<PowerQualityData>[] QualityMap =
        {
            // 0~11: 畸变值（跳过）
            new RegisterField<PowerQualityData>("CurrentTHDA", 12, RegisterValueType.Float32, 100f, "%", (d, v) => d.CurrentTHDA = v),
            new RegisterField<PowerQualityData>("CurrentTHDB", 16, RegisterValueType.Float32, 100f, "%", (d, v) => d.CurrentTHDB = v),
            new RegisterField<PowerQualityData>("CurrentTHDC", 20, RegisterValueType.Float32, 100f, "%", (d, v) => d.CurrentTHDC = v),
            // 24~47: 预留（跳过）；48~59: 畸变值（跳过）
            new RegisterField<PowerQualityData>("VoltageTHDA", 60, RegisterValueType.Float32, 100f, "%", (d, v) => d.VoltageTHDA = v),
            new RegisterField<PowerQualityData>("VoltageTHDB", 64, RegisterValueType.Float32, 100f, "%", (d, v) => d.VoltageTHDB = v),
            new RegisterField<PowerQualityData>("VoltageTHDC", 68, RegisterValueType.Float32, 100f, "%", (d, v) => d.VoltageTHDC = v),
            // 72~107: 预留（跳过，见上方 TODO）
            new RegisterField<PowerQualityData>("VoltageUnbalance", 108, RegisterValueType.Float32, 100f, "%", (d, v) => d.VoltageUnbalance = v),
            new RegisterField<PowerQualityData>("CurrentUnbalance", 112, RegisterValueType.Float32, 100f, "%", (d, v) => d.CurrentUnbalance = v)
        };

        internal static RegisterField<RealTimeData>[] RealtimeFields => RealtimeMap;
        internal static RegisterField<EnergyData>[] EnergyFields => EnergyMap;
        internal static RegisterField<PowerQualityData>[] QualityFields => QualityMap;

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
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, RealtimeStartAddress, 58);
            if (!IsResponseUsable(response, RegisterMapReader.RequiredLength(RealtimeMap), slaveAddress, "实时数据", RealtimeStartAddress))
            {
                return null;
            }

            var data = new RealTimeData();
            RegisterMapReader.Apply(RealtimeMap, response, data);
            return data;
        }

        /// <summary>
        /// 读取电能数据
        /// </summary>
        public EnergyData ReadEnergyData() => ReadEnergyData(SlaveAddress);

        public EnergyData ReadEnergyData(byte slaveAddress)
        {
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, EnergyStartAddress, 12);
            if (!IsResponseUsable(response, RegisterMapReader.RequiredLength(EnergyMap), slaveAddress, "电能数据", EnergyStartAddress))
            {
                return null;
            }

            var data = new EnergyData();
            RegisterMapReader.Apply(EnergyMap, response, data);
            return data;
        }

        /// <summary>
        /// 读取电能质量数据
        /// </summary>
        public PowerQualityData ReadPowerQualityData() => ReadPowerQualityData(SlaveAddress);

        public PowerQualityData ReadPowerQualityData(byte slaveAddress)
        {
            byte[] response = _modbusService.ReadHoldingRegisters(slaveAddress, QualityStartAddress, 58);
            if (!IsResponseUsable(response, RegisterMapReader.RequiredLength(QualityMap), slaveAddress, "电能质量", QualityStartAddress))
            {
                return null;
            }

            var data = new PowerQualityData();
            RegisterMapReader.Apply(QualityMap, response, data);
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
        internal static bool IsResponseUsable(byte[] response, int requiredBytes, byte slaveAddress, string what, ushort startAddress)
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
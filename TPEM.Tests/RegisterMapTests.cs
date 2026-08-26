using System;
using System.Linq;
using MeterAcquisition;
using Tpem.Protocol;
using Xunit;

namespace Tpem.Tests
{
    /// <summary>
    /// P2-1 映射表回归测试。
    ///
    /// 断言的是生产代码里真正在用的那两张映射表（通过 internal 暴露），不是测试里另抄一份。
    /// 这些用例的作用是把"偏移 + 缩放系数 + 单位"钉死：
    /// 以后谁改协议映射，只要动了字节偏移或量纲，这里就会红。
    /// 改造前这些值全是散落在解析代码里的裸字面量，改错了没有任何东西会提醒。
    /// </summary>
    public class RegisterMapTests
    {
        /// <summary>把大端 32 位浮点写进缓冲区，模拟线上字节。</summary>
        private static void WriteFloatBe(byte[] buffer, int offset, float value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            Array.Copy(bytes, 0, buffer, offset, 4);
        }

        private static void WriteInt32Be(byte[] buffer, int offset, int value)
        {
            var bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            Array.Copy(bytes, 0, buffer, offset, 4);
        }

        private static void WriteUInt16Be(byte[] buffer, int offset, ushort value)
        {
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)(value & 0xFF);
        }

        // ---------- 长度守卫（P0-2 的根因防回归）----------

        [Fact]
        public void 原有电表_三张映射表要求的长度与请求寄存器数一致()
        {
            // 改造前守卫写成 112 / 16 / 40，都小于实际消费量，导致响应偏短时解析越界。
            // 现在长度由映射表推导，这条测试确认推导值等于"请求寄存器数 × 2"。
            Assert.Equal(58 * 2, RegisterMapReader.RequiredLength(MeterDataService.RealtimeFields));
            Assert.Equal(12 * 2, RegisterMapReader.RequiredLength(MeterDataService.EnergyFields));
            Assert.Equal(58 * 2, RegisterMapReader.RequiredLength(MeterDataService.QualityFields));
        }

        [Fact]
        public void AMC96L_映射表要求的长度不超过请求寄存器数()
        {
            Assert.Equal(54 * 2, RegisterMapReader.RequiredLength(Amc96lE4MeterDataReader.RealtimeFields));
            Assert.Equal(10 * 2, RegisterMapReader.RequiredLength(Amc96lE4MeterDataReader.EnergyFields));
            Assert.Equal(6 * 2, RegisterMapReader.RequiredLength(Amc96lE4MeterDataReader.HarmonicFields));
            Assert.Equal(2 * 2, RegisterMapReader.RequiredLength(Amc96lE4MeterDataReader.UnbalanceFields));
        }

        [Fact]
        public void 映射表内不得有字段偏移重叠()
        {
            AssertNoOverlap(MeterDataService.RealtimeFields.Select(f => (f.Name, f.ByteOffset, f.EndOffsetExclusive)));
            AssertNoOverlap(MeterDataService.EnergyFields.Select(f => (f.Name, f.ByteOffset, f.EndOffsetExclusive)));
            AssertNoOverlap(MeterDataService.QualityFields.Select(f => (f.Name, f.ByteOffset, f.EndOffsetExclusive)));
            AssertNoOverlap(Amc96lE4MeterDataReader.RealtimeFields.Select(f => (f.Name, f.ByteOffset, f.EndOffsetExclusive)));
            AssertNoOverlap(Amc96lE4MeterDataReader.EnergyFields.Select(f => (f.Name, f.ByteOffset, f.EndOffsetExclusive)));
        }

        private static void AssertNoOverlap(System.Collections.Generic.IEnumerable<(string Name, int Start, int End)> fields)
        {
            var ordered = fields.OrderBy(f => f.Start).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                Assert.True(
                    ordered[i].Start >= ordered[i - 1].End,
                    $"字段 {ordered[i].Name}(起{ordered[i].Start}) 与 {ordered[i - 1].Name}(止{ordered[i - 1].End}) 重叠");
            }
        }

        // ---------- 原有电表：偏移与量纲 ----------

        [Fact]
        public void 原有电表_实时数据按映射表解析出正确的值与偏移()
        {
            var frame = new byte[116];
            WriteFloatBe(frame, 0, 220.1f);    // A 相电压
            WriteFloatBe(frame, 4, 219.9f);
            WriteFloatBe(frame, 8, 221.3f);
            WriteFloatBe(frame, 12, 999f);     // 平均相电压，应被跳过
            WriteFloatBe(frame, 16, 380.5f);   // AB 线电压
            WriteFloatBe(frame, 32, 12.5f);    // A 相电流
            WriteFloatBe(frame, 60, 45500f);   // 总有功功率 W
            WriteFloatBe(frame, 76, 1200f);    // 总无功
            WriteFloatBe(frame, 92, 45600f);   // 总视在
            WriteFloatBe(frame, 108, 0.93f);   // 总功率因数
            WriteFloatBe(frame, 112, 50.02f);  // 频率

            var data = new RealTimeData();
            RegisterMapReader.Apply(MeterDataService.RealtimeFields, frame, data);

            Assert.Equal(220.1f, data.VoltageA);
            Assert.Equal(219.9f, data.VoltageB);
            Assert.Equal(221.3f, data.VoltageC);
            Assert.Equal(380.5f, data.VoltageAB);
            Assert.Equal(12.5f, data.CurrentA);
            Assert.Equal(45500f, data.ActivePowerTotal);
            Assert.Equal(1200f, data.ReactivePowerTotal);
            Assert.Equal(45600f, data.ApparentPowerTotal);
            Assert.Equal(0.93f, data.PowerFactorTotal);
            Assert.Equal(50.02f, data.Frequency);
        }

        [Fact]
        public void 原有电表_电能是32位整数除以100()
        {
            var frame = new byte[24];
            WriteInt32Be(frame, 0, 123456);   // → 1234.56 kWh
            WriteInt32Be(frame, 4, 100);      // → 1.00 kWh
            WriteInt32Be(frame, 16, 5000);    // → 50.00 kvarh
            WriteInt32Be(frame, 20, 0);

            var data = new EnergyData();
            RegisterMapReader.Apply(MeterDataService.EnergyFields, frame, data);

            Assert.Equal(1234.56f, data.ForwardActiveEnergy.Value, 2);
            Assert.Equal(1f, data.ReverseActiveEnergy.Value, 2);
            Assert.Equal(50f, data.ForwardReactiveEnergy.Value, 2);
            Assert.Equal(0f, data.ReverseReactiveEnergy.Value, 2);
        }

        [Fact]
        public void 原有电表_质量项是比率乘100换成百分比()
        {
            var frame = new byte[116];
            WriteFloatBe(frame, 12, 0.032f);   // A 相电流 THD → 3.2 %
            WriteFloatBe(frame, 60, 0.021f);   // A 相电压 THD → 2.1 %
            WriteFloatBe(frame, 108, 0.015f);  // 电压不平衡 → 1.5 %
            WriteFloatBe(frame, 112, 0.028f);  // 电流不平衡 → 2.8 %

            var data = new PowerQualityData();
            RegisterMapReader.Apply(MeterDataService.QualityFields, frame, data);

            Assert.Equal(3.2f, data.CurrentTHDA.Value, 3);
            Assert.Equal(2.1f, data.VoltageTHDA.Value, 3);
            Assert.Equal(1.5f, data.VoltageUnbalance.Value, 3);
            Assert.Equal(2.8f, data.CurrentUnbalance.Value, 3);
        }

        // ---------- AMC96L-E4：偏移与量纲 ----------

        [Fact]
        public void AMC96L_功率原始单位是kW映射时乘1000换成W()
        {
            var frame = new byte[108];
            WriteFloatBe(frame, 0, 219.5f);   // A 相电压
            WriteFloatBe(frame, 52, 45.5f);   // 总有功 45.5 kW
            WriteFloatBe(frame, 68, 1.2f);    // 总无功 1.2 kvar
            WriteFloatBe(frame, 84, 45.6f);   // 总视在 45.6 kVA
            WriteFloatBe(frame, 100, 0.99f);  // 功率因数
            WriteFloatBe(frame, 104, 49.98f); // 频率

            var data = new RealTimeData();
            RegisterMapReader.Apply(Amc96lE4MeterDataReader.RealtimeFields, frame, data);

            Assert.Equal(219.5f, data.VoltageA);
            Assert.Equal(45500f, data.ActivePowerTotal.Value, 1);
            Assert.Equal(1200f, data.ReactivePowerTotal.Value, 1);
            Assert.Equal(45600f, data.ApparentPowerTotal.Value, 1);
            Assert.Equal(0.99f, data.PowerFactorTotal);
            Assert.Equal(49.98f, data.Frequency);
        }

        [Fact]
        public void AMC96L_谐波是单寄存器整数按001换算成百分比()
        {
            var frame = new byte[12];
            WriteUInt16Be(frame, 0, 320);   // 电压 THD A → 3.20 %
            WriteUInt16Be(frame, 6, 810);   // 电流 THD A → 8.10 %

            var data = new PowerQualityData();
            RegisterMapReader.Apply(Amc96lE4MeterDataReader.HarmonicFields, frame, data);

            Assert.Equal(3.2f, data.VoltageTHDA.Value, 3);
            Assert.Equal(8.1f, data.CurrentTHDA.Value, 3);
        }

        [Fact]
        public void AMC96L_不平衡度按01换算成百分比()
        {
            var frame = new byte[4];
            WriteUInt16Be(frame, 0, 21);  // → 2.1 %
            WriteUInt16Be(frame, 2, 35);  // → 3.5 %

            var data = new PowerQualityData();
            RegisterMapReader.Apply(Amc96lE4MeterDataReader.UnbalanceFields, frame, data);

            Assert.Equal(2.1f, data.VoltageUnbalance.Value, 3);
            Assert.Equal(3.5f, data.CurrentUnbalance.Value, 3);
        }

        // ---------- 两种协议落进同一批列时量纲必须一致 ----------

        [Fact]
        public void 两种协议对同一物理量给出相同单位的结果()
        {
            // 这是审查里最担心的一点：两种表的数据写进同一批数据库列，
            // 若量纲不同则网页端的风险统计必然不准。
            // 用"同一个物理量、各自协议的原始编码"造帧，断言解出来的业务值相同。

            // 3.2% 的电压 THD：原有电表编码为比率 0.032，AMC96L 编码为整数 320
            var legacyFrame = new byte[116];
            WriteFloatBe(legacyFrame, 60, 0.032f);
            var legacy = new PowerQualityData();
            RegisterMapReader.Apply(MeterDataService.QualityFields, legacyFrame, legacy);

            var amcFrame = new byte[12];
            WriteUInt16Be(amcFrame, 0, 320);
            var amc = new PowerQualityData();
            RegisterMapReader.Apply(Amc96lE4MeterDataReader.HarmonicFields, amcFrame, amc);

            Assert.Equal(legacy.VoltageTHDA.Value, amc.VoltageTHDA.Value, 3);

            // 45.5 kW 的总有功：原有电表原始即 W，AMC96L 原始为 kW
            var legacyRt = new byte[116];
            WriteFloatBe(legacyRt, 60, 45500f);
            var legacyPower = new RealTimeData();
            RegisterMapReader.Apply(MeterDataService.RealtimeFields, legacyRt, legacyPower);

            var amcRt = new byte[108];
            WriteFloatBe(amcRt, 52, 45.5f);
            var amcPower = new RealTimeData();
            RegisterMapReader.Apply(Amc96lE4MeterDataReader.RealtimeFields, amcRt, amcPower);

            Assert.Equal(legacyPower.ActivePowerTotal.Value, amcPower.ActivePowerTotal.Value, 1);
        }

        [Fact]
        public void 报文偏短时只影响越界字段其余字段仍可用()
        {
            // P0-2 的另一半：即使守卫放过了偏短的帧，单个字段越界也不再抛异常，
            // 而是该字段留 null（P1-5 语义）。
            var shortFrame = new byte[64];
            WriteFloatBe(shortFrame, 0, 220f);
            WriteFloatBe(shortFrame, 60, 45500f);

            var data = new RealTimeData();
            RegisterMapReader.Apply(MeterDataService.RealtimeFields, shortFrame, data);

            Assert.Equal(220f, data.VoltageA);
            Assert.Equal(45500f, data.ActivePowerTotal);
            Assert.Null(data.PowerFactorTotal);  // 偏移 108，越界
            Assert.Null(data.Frequency);         // 偏移 112，越界
        }
    }
}

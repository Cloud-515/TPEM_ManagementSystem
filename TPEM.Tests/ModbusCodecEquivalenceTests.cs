using System;
using Tpem.Protocol;
using Xunit;

namespace Tpem.Tests
{
    /// <summary>
    /// P2-1 的等价性回归测试。
    ///
    /// 目的不是"验证新代码好看"，而是**锁定改造前的实际行为**：
    /// 下面两个 Legacy_ / Amc_ 方法逐字复刻了改造前两个读取器里的私有解码逻辑，
    /// 测试断言 ModbusCodec 与它们产出完全相同的值。
    /// 这样"抽出公共实现"这件事才是可证明的等价替换，而不是凭感觉重写。
    /// </summary>
    public class ModbusCodecEquivalenceTests
    {
        /// <summary>改造前 MeterDataService.GetReversedBytes + BitConverter.ToSingle 的原始写法。</summary>
        private static float LegacyReadFloat(byte[] source, int offset)
        {
            var temp = new[] { source[offset + 3], source[offset + 2], source[offset + 1], source[offset] };
            return BitConverter.ToSingle(temp, 0);
        }

        /// <summary>改造前 Amc96lE4MeterDataReader.ParseFloat 的原始写法（先按大端组寄存器，再组 uint）。</summary>
        private static float AmcReadFloat(byte[] source, int offset)
        {
            var hi = (ushort)((source[offset] << 8) | source[offset + 1]);
            var lo = (ushort)((source[offset + 2] << 8) | source[offset + 3]);
            var raw = ((uint)hi << 16) | lo;
            return BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
        }

        public static TheoryData<float> SampleValues => new TheoryData<float>
        {
            0f, 1f, -1f, 50f, 219.87f, 242f, 0.85f, -0.85f,
            400.5f, 1234.5678f, float.Epsilon, -12345.678f, 0.0001f
        };

        [Theory]
        [MemberData(nameof(SampleValues))]
        public void ReadFloat32_Abcd_与改造前两套实现结果一致(float expected)
        {
            // 造出大端（ABCD）线上字节，两种电表实测都是这个顺序
            var wire = BitConverter.GetBytes(expected);
            Array.Reverse(wire);

            var legacy = LegacyReadFloat(wire, 0);
            var amc = AmcReadFloat(wire, 0);
            var codec = ModbusCodec.ReadFloat32(wire, 0, Modbus32BitOrder.Abcd);

            Assert.Equal(expected, legacy);
            Assert.Equal(expected, amc);
            Assert.True(codec.HasValue);
            Assert.Equal(expected, codec.Value);
        }

        [Fact]
        public void 两套改造前实现本身就是等价的_确认量纲差异不来自字节序()
        {
            // 审查初稿里写过"两种协议字节序各写一套"，容易被读成"结果不同"。
            // 这条测试把事实钉住：写法不同，结果完全相同，都等价于大端。
            // 真正的差异只在缩放系数（因为两种表的原始编码不同），见 RegisterScalingTests。
            var random = new Random(20260826);
            for (var i = 0; i < 500; i++)
            {
                var wire = new byte[4];
                random.NextBytes(wire);

                var legacy = LegacyReadFloat(wire, 0);
                var amc = AmcReadFloat(wire, 0);

                if (float.IsNaN(legacy))
                {
                    Assert.True(float.IsNaN(amc));
                    continue;
                }

                Assert.Equal(legacy, amc);
            }
        }

        [Theory]
        [InlineData(Modbus32BitOrder.Abcd, new byte[] { 0x43, 0x5C, 0x00, 0x00 })]
        [InlineData(Modbus32BitOrder.Dcba, new byte[] { 0x00, 0x00, 0x5C, 0x43 })]
        [InlineData(Modbus32BitOrder.Badc, new byte[] { 0x5C, 0x43, 0x00, 0x00 })]
        [InlineData(Modbus32BitOrder.Cdab, new byte[] { 0x00, 0x00, 0x43, 0x5C })]
        public void ReadFloat32_四种字节序都能还原同一个值(Modbus32BitOrder order, byte[] wire)
        {
            var value = ModbusCodec.ReadFloat32(wire, 0, order);
            Assert.True(value.HasValue);
            Assert.Equal(220f, value.Value);
        }

        [Fact]
        public void ReadFloat32_越界返回null而不是抛异常()
        {
            var wire = new byte[] { 0x43, 0x5C, 0x00 };
            Assert.Null(ModbusCodec.ReadFloat32(wire, 0, Modbus32BitOrder.Abcd));
            Assert.Null(ModbusCodec.ReadFloat32(wire, -1, Modbus32BitOrder.Abcd));
            Assert.Null(ModbusCodec.ReadFloat32(null, 0, Modbus32BitOrder.Abcd));
        }

        [Fact]
        public void ReadFloat32_NaN与无穷按缺数据处理()
        {
            // 0x7FC00000 = NaN，0x7F800000 = +∞。改造前这两个会被当成真实测量值写进数据库。
            var nan = new byte[] { 0x7F, 0xC0, 0x00, 0x00 };
            var inf = new byte[] { 0x7F, 0x80, 0x00, 0x00 };
            Assert.Null(ModbusCodec.ReadFloat32(nan, 0, Modbus32BitOrder.Abcd));
            Assert.Null(ModbusCodec.ReadFloat32(inf, 0, Modbus32BitOrder.Abcd));
        }

        [Fact]
        public void ReadInt32_与改造前电能读取的写法一致()
        {
            // 改造前 ReadEnergyData：BitConverter.ToInt32(GetReversedBytes(...)) / 100.0f
            var wire = new byte[] { 0x00, 0x01, 0x86, 0xA0 }; // 100000
            var codec = ModbusCodec.ReadInt32(wire, 0, Modbus32BitOrder.Abcd);
            Assert.True(codec.HasValue);
            Assert.Equal(100000, codec.Value);
            Assert.Equal(1000f, ModbusCodec.Scale(codec, 0.01f));
        }

        [Fact]
        public void ReadUInt16_按大端解析单寄存器()
        {
            var wire = new byte[] { 0x01, 0xF4 }; // 500
            Assert.Equal((ushort)500, ModbusCodec.ReadUInt16(wire, 0));
            Assert.Null(ModbusCodec.ReadUInt16(wire, 1));
        }

        [Fact]
        public void Scale_缺数据不会被换算成0()
        {
            Assert.Null(ModbusCodec.Scale((float?)null, 100f));
            Assert.Null(ModbusCodec.Scale((int?)null, 0.01f));
            Assert.Null(ModbusCodec.Scale((ushort?)null, 0.01f));
            Assert.Equal(5f, ModbusCodec.Scale((ushort?)500, 0.01f));
        }
    }
}

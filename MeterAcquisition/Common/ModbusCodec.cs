using System;

namespace Tpem.Protocol
{
    /// <summary>
    /// 32 位量的字节序（以寄存器/字节在线上的出现顺序命名）。
    /// A 为最先出现的字节，D 为最后出现的字节。
    /// </summary>
    public enum Modbus32BitOrder
    {
        /// <summary>大端：先高字节。两种电表实测都是这种。</summary>
        Abcd = 0,

        /// <summary>小端。</summary>
        Dcba = 1,

        /// <summary>字内交换（大端字序、小端字节序）。</summary>
        Badc = 2,

        /// <summary>字交换（小端字序、大端字节序），部分仪表用这种。</summary>
        Cdab = 3
    }

    /// <summary>
    /// Modbus 报文字节解码（P2-1）。
    ///
    /// 为什么要有这一层：改造前 Legacy 与 AMC96L-E4 两个读取器各写了一套 32 位解码 ——
    /// 前者是 `GetReversedBytes` 反转 4 字节后 BitConverter，后者是 `(reg[i]&lt;&lt;16)|reg[i+1]` 再
    /// BitConverter。经等价性测试确认两者结果相同（都等价于大端 ABCD），
    /// 但同一件事写两遍、都没有测试、都没有边界检查，任何一处笔误都会静默产出错误的电力数据。
    /// 现在只保留这一份实现，并且全部带范围检查与 NaN/无穷过滤。
    ///
    /// 约定：越界或解析出 NaN/无穷时返回 null（配合 P1-5 的可空语义），
    /// 绝不返回 0 —— 0 在电力量里是有意义的测量值，不能用来表示"没读到"。
    /// </summary>
    public static class ModbusCodec
    {
        /// <summary>把 4 个连续字节按指定字节序重排成 CLR 期望的小端顺序。</summary>
        private static bool TryReorder32(byte[] data, int offset, Modbus32BitOrder order, byte[] target)
        {
            if (data == null || offset < 0 || offset + 4 > data.Length)
            {
                return false;
            }

            byte a = data[offset];
            byte b = data[offset + 1];
            byte c = data[offset + 2];
            byte d = data[offset + 3];

            switch (order)
            {
                case Modbus32BitOrder.Abcd:
                    target[0] = d; target[1] = c; target[2] = b; target[3] = a;
                    break;
                case Modbus32BitOrder.Dcba:
                    target[0] = a; target[1] = b; target[2] = c; target[3] = d;
                    break;
                case Modbus32BitOrder.Badc:
                    target[0] = c; target[1] = d; target[2] = a; target[3] = b;
                    break;
                case Modbus32BitOrder.Cdab:
                    target[0] = b; target[1] = a; target[2] = d; target[3] = c;
                    break;
                default:
                    return false;
            }

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(target);
            }

            return true;
        }

        /// <summary>读 32 位 IEEE754 浮点。NaN / 无穷视为无效，返回 null。</summary>
        public static float? ReadFloat32(byte[] data, int offset, Modbus32BitOrder order)
        {
            var buffer = new byte[4];
            if (!TryReorder32(data, offset, order, buffer))
            {
                return null;
            }

            var value = BitConverter.ToSingle(buffer, 0);
            return float.IsNaN(value) || float.IsInfinity(value) ? (float?)null : value;
        }

        /// <summary>读 32 位有符号整数。</summary>
        public static int? ReadInt32(byte[] data, int offset, Modbus32BitOrder order)
        {
            var buffer = new byte[4];
            return TryReorder32(data, offset, order, buffer)
                ? BitConverter.ToInt32(buffer, 0)
                : (int?)null;
        }

        /// <summary>读 32 位无符号整数。</summary>
        public static uint? ReadUInt32(byte[] data, int offset, Modbus32BitOrder order)
        {
            var buffer = new byte[4];
            return TryReorder32(data, offset, order, buffer)
                ? BitConverter.ToUInt32(buffer, 0)
                : (uint?)null;
        }

        /// <summary>读单个寄存器（16 位无符号，Modbus 线上恒为大端）。</summary>
        public static ushort? ReadUInt16(byte[] data, int offset)
        {
            if (data == null || offset < 0 || offset + 2 > data.Length)
            {
                return null;
            }

            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        /// <summary>读单个寄存器（16 位有符号）。</summary>
        public static short? ReadInt16(byte[] data, int offset)
        {
            var raw = ReadUInt16(data, offset);
            return raw.HasValue ? (short)raw.Value : (short?)null;
        }

        /// <summary>
        /// 按缩放系数换算。value 为 null 时结果也是 null ——
        /// 缺数据不能因为乘了个系数就变成 0。
        /// </summary>
        public static float? Scale(float? value, float factor)
        {
            if (!value.HasValue)
            {
                return null;
            }

            var scaled = value.Value * factor;
            return float.IsNaN(scaled) || float.IsInfinity(scaled) ? (float?)null : scaled;
        }

        public static float? Scale(int? value, float factor)
        {
            return value.HasValue ? Scale((float)value.Value, factor) : (float?)null;
        }

        public static float? Scale(ushort? value, float factor)
        {
            return value.HasValue ? Scale((float)value.Value, factor) : (float?)null;
        }
    }
}

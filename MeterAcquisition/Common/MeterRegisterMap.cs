using System;
using System.Collections.Generic;

namespace Tpem.Protocol
{
    /// <summary>寄存器字段的原始编码类型。</summary>
    public enum RegisterValueType
    {
        /// <summary>32 位 IEEE754 浮点，占 2 个寄存器。</summary>
        Float32 = 0,

        /// <summary>32 位有符号整数，占 2 个寄存器。</summary>
        Int32 = 1,

        /// <summary>16 位无符号整数，占 1 个寄存器。</summary>
        UInt16 = 2,

        /// <summary>16 位有符号整数，占 1 个寄存器。</summary>
        Int16 = 3
    }

    /// <summary>
    /// 一个寄存器字段的映射描述（P2-1）。
    ///
    /// 改造前的问题不是"解析算错了"，而是"解析规则散落在一长串 offset += 4 / *100 / /10 里"：
    /// 偏移、缩放系数、单位全是裸字面量，没有任何一处能被测试，
    /// 改协议表时只能靠人眼数字节，而且两种电表各数一遍。
    /// 现在把这些做成数据：偏移、类型、字节序、缩放、单位都是字段，映射表可以逐条断言。
    /// </summary>
    public sealed class RegisterField<T>
    {
        public RegisterField(
            string name,
            int byteOffset,
            RegisterValueType valueType,
            float scale,
            string unit,
            Action<T, float?> assign,
            Modbus32BitOrder order = Modbus32BitOrder.Abcd)
        {
            if (assign == null)
            {
                throw new ArgumentNullException(nameof(assign));
            }

            Name = name;
            ByteOffset = byteOffset;
            ValueType = valueType;
            Scale = scale;
            Unit = unit;
            Assign = assign;
            Order = order;
        }

        /// <summary>字段名，仅用于日志与测试断言。</summary>
        public string Name { get; private set; }

        /// <summary>相对于本次读取返回数据区起点的字节偏移。</summary>
        public int ByteOffset { get; private set; }

        public RegisterValueType ValueType { get; private set; }

        /// <summary>缩放系数：业务值 = 原始值 × Scale。</summary>
        public float Scale { get; private set; }

        /// <summary>业务值的单位，写在映射表里而不是散落在注释中。</summary>
        public string Unit { get; private set; }

        public Modbus32BitOrder Order { get; private set; }

        public Action<T, float?> Assign { get; private set; }

        /// <summary>该字段消费到的最后一个字节的下一位，用于推导整块所需长度。</summary>
        public int EndOffsetExclusive
        {
            get
            {
                var size = ValueType == RegisterValueType.UInt16 || ValueType == RegisterValueType.Int16 ? 2 : 4;
                return ByteOffset + size;
            }
        }
    }

    /// <summary>按映射表解析整块寄存器数据。</summary>
    public static class RegisterMapReader
    {
        /// <summary>
        /// 映射表要求的最小数据长度。
        /// P0-2 里那三处长度守卫偏小的问题，根因就是"实际消费到哪个字节"靠人算；
        /// 现在由映射表自己算出来，不会再和解析代码脱节。
        /// </summary>
        public static int RequiredLength<T>(IEnumerable<RegisterField<T>> fields)
        {
            var max = 0;
            foreach (var field in fields)
            {
                if (field.EndOffsetExclusive > max)
                {
                    max = field.EndOffsetExclusive;
                }
            }

            return max;
        }

        /// <summary>
        /// 把数据区按映射表写入目标对象。
        /// 单个字段越界或解析无效时该字段置 null（P1-5 语义），不影响其余字段，也不抛异常。
        /// </summary>
        public static void Apply<T>(IEnumerable<RegisterField<T>> fields, byte[] data, T target)
        {
            if (fields == null || target == null)
            {
                return;
            }

            foreach (var field in fields)
            {
                field.Assign(target, ReadField(field, data));
            }
        }

        /// <summary>按单个字段描述解析出业务值（已含缩放）。</summary>
        public static float? ReadField<T>(RegisterField<T> field, byte[] data)
        {
            if (field == null)
            {
                return null;
            }

            switch (field.ValueType)
            {
                case RegisterValueType.Float32:
                    return ModbusCodec.Scale(ModbusCodec.ReadFloat32(data, field.ByteOffset, field.Order), field.Scale);
                case RegisterValueType.Int32:
                    return ModbusCodec.Scale(ModbusCodec.ReadInt32(data, field.ByteOffset, field.Order), field.Scale);
                case RegisterValueType.UInt16:
                    return ModbusCodec.Scale(ModbusCodec.ReadUInt16(data, field.ByteOffset), field.Scale);
                case RegisterValueType.Int16:
                    var signed = ModbusCodec.ReadInt16(data, field.ByteOffset);
                    return signed.HasValue ? ModbusCodec.Scale((float)signed.Value, field.Scale) : (float?)null;
                default:
                    return null;
            }
        }
    }
}

namespace MeterAcquisition
{
    /// <summary>
    /// CRC16 校验工具类
    /// </summary>
    public static class Crc16Helper
    {
        /// <summary>
        /// 计算 Modbus CRC16 校验值
        /// </summary>
        /// <param name="data">数据字节数组</param>
        /// <param name="length">计算长度</param>
        /// <returns>CRC16 值</returns>
        public static ushort Calculate(byte[] data, int length)
        {
            return Calculate(data, 0, length);
        }

        /// <summary>
        /// 计算 Modbus CRC16 校验值（指定起始偏移）
        /// </summary>
        /// <param name="data">数据字节数组</param>
        /// <param name="offset">起始偏移</param>
        /// <param name="length">计算长度</param>
        /// <returns>CRC16 值</returns>
        public static ushort Calculate(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[offset + i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }

        /// <summary>
        /// 验证 CRC16 校验
        /// </summary>
        /// <param name="data">包含 CRC 的数据</param>
        /// <param name="length">数据总长度（包含2字节CRC）</param>
        /// <returns>校验是否正确</returns>
        public static bool Validate(byte[] data, int length)
        {
            return Validate(data, 0, length);
        }

        /// <summary>
        /// 验证 CRC16 校验（指定起始偏移）。
        /// 用于在"缓冲区里可能有多帧"时校验其中某一帧，而不是从缓冲区头部硬算。
        /// </summary>
        /// <param name="data">包含 CRC 的数据</param>
        /// <param name="offset">帧起始偏移</param>
        /// <param name="length">该帧总长度（包含2字节CRC）</param>
        /// <returns>校验是否正确</returns>
        public static bool Validate(byte[] data, int offset, int length)
        {
            if (length < 3) return false;
            if (offset < 0 || offset + length > data.Length) return false;

            ushort receivedCrc = (ushort)(data[offset + length - 1] << 8 | data[offset + length - 2]);
            ushort calculatedCrc = Calculate(data, offset, length - 2);

            return receivedCrc == calculatedCrc;
        }
    }
}
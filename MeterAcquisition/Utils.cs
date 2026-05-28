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
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
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
            if (length < 3) return false;

            ushort receivedCrc = (ushort)(data[length - 1] << 8 | data[length - 2]);
            ushort calculatedCrc = Calculate(data, length - 2);

            return receivedCrc == calculatedCrc;
        }
    }
}
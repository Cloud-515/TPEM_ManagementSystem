using System.IO.Ports;
using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Services;

public sealed class ModbusRtuClient : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Queue<ModbusFrameRecord> _recentFrames = new();
    private readonly object _frameLock = new();
    private SerialPort? _serialPort;

    public event EventHandler<ModbusFrameRecord>? FrameObserved;

    public bool IsOpen => _serialPort?.IsOpen == true;

    public string PortName => _serialPort?.PortName ?? string.Empty;

    public IReadOnlyList<ModbusFrameRecord> GetRecentFrames()
    {
        lock (_frameLock)
        {
            return _recentFrames.ToArray();
        }
    }

    public async Task<bool> ConnectAsync(CommSettings settings, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DisconnectCore();

            var parity = Enum.TryParse<Parity>(settings.Parity, true, out var parsedParity)
                ? parsedParity
                : Parity.None;
            var stopBits = settings.StopBits == 2 ? StopBits.Two : StopBits.One;

            var serialPort = new SerialPort(settings.PortName, settings.BaudRate, parity, settings.DataBits, stopBits)
            {
                ReadTimeout = settings.ReadTimeoutMs,
                WriteTimeout = settings.WriteTimeoutMs,
            };

            serialPort.Open();
            _serialPort = serialPort;
            return true;
        }
        catch
        {
            DisconnectCore();
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DisconnectCore();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveAddress, ushort startAddress, ushort registerCount, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            EnsureConnected();
            var serialPort = _serialPort!;
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();

            var request = BuildReadHoldingRegistersFrame(slaveAddress, startAddress, registerCount);
            serialPort.Write(request, 0, request.Length);
            RecordFrame("TX", request);

            var response = ReadResponse(serialPort, slaveAddress, 0x03);
            RecordFrame("RX", response);
            ValidateResponse(response, slaveAddress, 0x03, registerCount);

            var values = new ushort[registerCount];
            for (var index = 0; index < registerCount; index++)
            {
                var dataOffset = 3 + index * 2;
                values[index] = (ushort)((response[dataOffset] << 8) | response[dataOffset + 1]);
            }

            return values;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> ProbeAddressAsync(byte slaveAddress, ushort startAddress, ushort registerCount, CancellationToken cancellationToken = default)
    {
        try
        {
            await ReadHoldingRegistersAsync(slaveAddress, startAddress, registerCount, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task WriteSingleRegisterAsync(byte slaveAddress, ushort registerAddress, ushort value, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            EnsureConnected();
            var serialPort = _serialPort!;
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();

            var request = BuildWriteSingleRegisterFrame(slaveAddress, registerAddress, value);
            serialPort.Write(request, 0, request.Length);
            RecordFrame("TX", request);

            var response = ReadResponse(serialPort, slaveAddress, 0x06);
            RecordFrame("RX", response);
            ValidateWriteSingleRegisterResponse(response, slaveAddress, registerAddress, value);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        DisconnectCore();
        _gate.Dispose();
    }

    private void EnsureConnected()
    {
        if (_serialPort?.IsOpen != true)
        {
            throw new InvalidOperationException("串口尚未连接。");
        }
    }

    private void DisconnectCore()
    {
        if (_serialPort is null)
        {
            return;
        }

        try
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
        finally
        {
            _serialPort.Dispose();
            _serialPort = null;
        }
    }

    private void RecordFrame(string direction, byte[] frame)
    {
        var record = new ModbusFrameRecord(DateTime.Now, direction, BitConverter.ToString(frame).Replace("-", " "));
        lock (_frameLock)
        {
            _recentFrames.Enqueue(record);
            while (_recentFrames.Count > 40)
            {
                _recentFrames.Dequeue();
            }
        }

        FrameObserved?.Invoke(this, record);
    }

    private static byte[] BuildReadHoldingRegistersFrame(byte slaveAddress, ushort startAddress, ushort registerCount)
    {
        var frame = new byte[8];
        frame[0] = slaveAddress;
        frame[1] = 0x03;
        frame[2] = (byte)(startAddress >> 8);
        frame[3] = (byte)(startAddress & 0xFF);
        frame[4] = (byte)(registerCount >> 8);
        frame[5] = (byte)(registerCount & 0xFF);

        var crc = ComputeCrc(frame.AsSpan(0, 6));
        frame[6] = (byte)(crc & 0xFF);
        frame[7] = (byte)(crc >> 8);
        return frame;
    }

    private static byte[] BuildWriteSingleRegisterFrame(byte slaveAddress, ushort registerAddress, ushort value)
    {
        var frame = new byte[8];
        frame[0] = slaveAddress;
        frame[1] = 0x06;
        frame[2] = (byte)(registerAddress >> 8);
        frame[3] = (byte)(registerAddress & 0xFF);
        frame[4] = (byte)(value >> 8);
        frame[5] = (byte)(value & 0xFF);

        var crc = ComputeCrc(frame.AsSpan(0, 6));
        frame[6] = (byte)(crc & 0xFF);
        frame[7] = (byte)(crc >> 8);
        return frame;
    }

    private static byte[] ReadResponse(SerialPort serialPort, byte slaveAddress, byte functionCode)
    {
        var header = ReadExact(serialPort, 2);
        if (header[0] != slaveAddress)
        {
            throw new InvalidOperationException("Modbus RTU 响应地址不匹配。");
        }

        if (header[1] == (functionCode | 0x80))
        {
            var exceptionFrame = new byte[5];
            exceptionFrame[0] = header[0];
            exceptionFrame[1] = header[1];
            ReadExact(serialPort, exceptionFrame, 2, 3);
            ValidateCrc(exceptionFrame);
            throw new InvalidOperationException($"Modbus RTU 返回异常码 {exceptionFrame[2]}。");
        }

        if (header[1] != functionCode)
        {
            throw new InvalidOperationException("Modbus RTU 功能码不匹配。");
        }

        if (functionCode == 0x06)
        {
            var response = new byte[8];
            response[0] = header[0];
            response[1] = header[1];
            ReadExact(serialPort, response, 2, 6);
            return response;
        }

        var byteCount = ReadExact(serialPort, 1)[0];
        var responseWithData = new byte[3 + byteCount + 2];
        responseWithData[0] = header[0];
        responseWithData[1] = header[1];
        responseWithData[2] = byteCount;
        ReadExact(serialPort, responseWithData, 3, byteCount + 2);
        return responseWithData;
    }

    private static byte[] ReadExact(SerialPort serialPort, int length)
    {
        var buffer = new byte[length];
        ReadExact(serialPort, buffer, 0, length);
        return buffer;
    }

    private static void ReadExact(SerialPort serialPort, byte[] buffer, int offset, int count)
    {
        var end = offset + count;
        while (offset < end)
        {
            try
            {
                var read = serialPort.Read(buffer, offset, end - offset);
                if (read <= 0)
                {
                    throw new TimeoutException("Modbus RTU 响应超时。");
                }

                offset += read;
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException("Modbus RTU 响应超时。", ex);
            }
        }
    }

    private static void ValidateCrc(byte[] response)
    {
        var expectedCrc = ComputeCrc(response.AsSpan(0, response.Length - 2));
        var actualCrc = (ushort)(response[response.Length - 2] | (response[response.Length - 1] << 8));
        if (expectedCrc != actualCrc)
        {
            throw new InvalidOperationException("Modbus RTU CRC 校验失败。");
        }
    }

    private static void ValidateWriteSingleRegisterResponse(byte[] response, byte slaveAddress, ushort registerAddress, ushort value)
    {
        if (response.Length != 8)
        {
            throw new InvalidOperationException("Modbus RTU 写单寄存器响应长度无效。");
        }

        if (response[0] != slaveAddress || response[1] != 0x06)
        {
            throw new InvalidOperationException("Modbus RTU 写单寄存器响应头无效。");
        }

        var echoedAddress = (ushort)((response[2] << 8) | response[3]);
        var echoedValue = (ushort)((response[4] << 8) | response[5]);
        if (echoedAddress != registerAddress || echoedValue != value)
        {
            throw new InvalidOperationException("Modbus RTU 写单寄存器回显不匹配。");
        }

        var expectedCrc = ComputeCrc(response.AsSpan(0, response.Length - 2));
        var actualCrc = (ushort)(response[response.Length - 2] | (response[response.Length - 1] << 8));
        if (expectedCrc != actualCrc)
        {
            throw new InvalidOperationException("Modbus RTU 写单寄存器 CRC 校验失败。");
        }
    }

    private static void ValidateResponse(byte[] response, byte slaveAddress, byte functionCode, ushort registerCount)
    {
        if (response.Length < 5)
        {
            throw new InvalidOperationException("Modbus RTU 响应长度无效。");
        }

        if (response[0] != slaveAddress)
        {
            throw new InvalidOperationException("Modbus RTU 响应地址不匹配。");
        }

        if (response[1] == (functionCode | 0x80))
        {
            throw new InvalidOperationException($"Modbus RTU 返回异常码 {response[2]}。");
        }

        if (response[1] != functionCode)
        {
            throw new InvalidOperationException("Modbus RTU 功能码不匹配。");
        }

        var expectedByteCount = registerCount * 2;
        if (response[2] != expectedByteCount)
        {
            throw new InvalidOperationException("Modbus RTU 数据长度不匹配。");
        }

        var expectedCrc = ComputeCrc(response.AsSpan(0, response.Length - 2));
        var actualCrc = (ushort)(response[response.Length - 2] | (response[response.Length - 1] << 8));
        if (expectedCrc != actualCrc)
        {
            throw new InvalidOperationException("Modbus RTU CRC 校验失败。");
        }
    }

    private static ushort ComputeCrc(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                var lsb = (crc & 0x0001) != 0;
                crc >>= 1;
                if (lsb)
                {
                    crc ^= 0xA001;
                }
            }
        }

        return crc;
    }
}

public sealed class ModbusFrameRecord
{
    public ModbusFrameRecord(DateTime timestamp, string direction, string hex)
    {
        Timestamp = timestamp;
        Direction = direction;
        Hex = hex;
    }

    public DateTime Timestamp { get; }
    public string Direction { get; }
    public string Hex { get; }
}

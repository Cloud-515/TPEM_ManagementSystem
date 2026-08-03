namespace MeterAcquisition.HeatPump.Domain;

public sealed class CommSettings
{
    public byte SlaveAddress { get; set; } = 1;

    public string PortName { get; set; } = string.Empty;

    public int BaudRate { get; set; } = 9600;

    public int DataBits { get; set; } = 8;

    public int StopBits { get; set; } = 1;

    public string Parity { get; set; } = "None";

    public int ReadTimeoutMs { get; set; } = 1000;

    public int WriteTimeoutMs { get; set; } = 1000;

    public byte ControllerScanStartAddress { get; set; } = 1;

    public byte ControllerScanEndAddress { get; set; } = 16;
}

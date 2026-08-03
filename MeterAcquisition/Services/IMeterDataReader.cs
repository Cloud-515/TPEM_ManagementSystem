namespace MeterAcquisition
{
    public interface IMeterDataReader
    {
        string DeviceModel { get; }

        bool Probe(byte slaveAddress);
        RealTimeData ReadRealTimeData();
        RealTimeData ReadRealTimeData(byte slaveAddress);
        EnergyData ReadEnergyData();
        EnergyData ReadEnergyData(byte slaveAddress);
        PowerQualityData ReadPowerQualityData();
        PowerQualityData ReadPowerQualityData(byte slaveAddress);
    }
}

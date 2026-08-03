using MeterAcquisition.HeatPump.Domain;

namespace MeterAcquisition.HeatPump.Application;

public sealed class HeatPumpTelemetryRefreshResult
{
    public HeatPumpTelemetryRefreshResult(HeatPumpModuleInfo module, Exception error = null)
    {
        Module = module;
        Error = error;
    }

    public HeatPumpModuleInfo Module { get; }

    public Exception Error { get; }

    public bool IsSuccess => Error == null && Module?.Telemetry.LastUpdatedAt != default(DateTime);
}

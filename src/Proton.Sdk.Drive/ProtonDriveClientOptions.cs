using Proton.Sdk.Instrumentation.Metrics;

namespace Proton.Sdk.Drive;

public readonly struct ProtonDriveClientOptions
{
    public Meter? InstrumentationMeter { get; init; }
    public string? ClientId { get; init; }
}

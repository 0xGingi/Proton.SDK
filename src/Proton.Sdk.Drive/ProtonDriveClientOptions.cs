using Proton.Sdk.Instrumentation;

namespace Proton.Sdk.Drive;

public readonly struct ProtonDriveClientOptions
{
    public IInstrumentFactory? InstrumentFactory { get; init; }
    public string? ClientId { get; init; }
}

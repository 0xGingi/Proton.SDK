namespace Proton.Sdk.Instrumentation;

internal static class ProtonInstrumentationDefaults
{
    public const string ObservabilityBaseRoute = "data/";

    public static readonly TimeSpan DefaultReportingInterval = TimeSpan.FromSeconds(10);
}

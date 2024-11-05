using System.Text.Json.Serialization;
using Proton.Sdk.Instrumentation.Provider;

namespace Proton.Sdk.Instrumentation;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(ObservabilityMetricsParameters))]
internal partial class ProtonInstrumentationApiSerializerContext : JsonSerializerContext
{
    static ProtonInstrumentationApiSerializerContext()
    {
        Default = new ProtonInstrumentationApiSerializerContext(ProtonApiDefaults.GetSerializerOptions());
    }
}

using System.Text.Json.Serialization;
using Proton.Sdk.Drive.Instrumentation.Metrics;

namespace Proton.Sdk.Drive.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UploadSuccessRateMetricLabels))]
internal partial class ProtonDriveInstrumentationSerializerContext : JsonSerializerContext;

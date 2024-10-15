using System.Text.Json.Serialization;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Authentication;

public readonly struct SecondFactorParameters
{
    [JsonPropertyName("Enabled")]
    [JsonConverter(typeof(BooleanToIntegerJsonConverter))]
    public required bool IsEnabled { get; init; }
}

using System.Text.Json.Serialization;

namespace Proton.Sdk.Authentication;

internal sealed class ModulusResponse : ApiResponse
{
    public required string Modulus { get; set; }

    [JsonPropertyName("ModulusID")]
    public required string ModulusId { get; set; }
}

using System.Text.Json.Serialization;

namespace Proton.Sdk;

internal class ApiResponse
{
    public required ResponseCode Code { get; init; }

    [JsonPropertyName("Error")]
    public string? ErrorMessage { get; init; }

    public bool IsSuccess => Code is ResponseCode.Success;
}

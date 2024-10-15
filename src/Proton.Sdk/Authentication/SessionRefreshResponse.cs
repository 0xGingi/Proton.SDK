using System.Text.Json.Serialization;

namespace Proton.Sdk.Authentication;

internal sealed class SessionRefreshResponse : ApiResponse
{
    public required string AccessToken { get; init; }

    public string? TokenType { get; init; }

    public required IReadOnlyList<string> Scopes { get; init; }

    [JsonPropertyName("UID")]
    public required string SessionId { get; init; }

    public required string RefreshToken { get; init; }
}

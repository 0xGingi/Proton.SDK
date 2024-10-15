using System.Text.Json.Serialization;

namespace Proton.Sdk.Authentication;

internal struct SessionRefreshRequest
{
    public SessionRefreshRequest(string refreshToken)
    {
        RefreshToken = refreshToken;
    }

    public string RefreshToken { get; }

    public string ResponseType => "token";

    public string GrantType => "refresh_token";

    [JsonPropertyName("RedirectURI")]
    public string RedirectUri => "https://proton.me";
}

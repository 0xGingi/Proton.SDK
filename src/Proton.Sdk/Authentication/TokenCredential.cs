namespace Proton.Sdk.Authentication;

public sealed class TokenCredential
{
    public TokenCredential(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    public string AccessToken { get; }
    public string RefreshToken { get; }

    public ValueTask<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<string>(AccessToken);
    }
}

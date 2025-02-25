using Google.Protobuf;
using static System.Net.Http.HttpMethod;

namespace Proton.Sdk.CExports;

internal static class InteropTokenRefreshedCallbackExtensions
{
    internal static unsafe void TokensRefreshed(
        this InteropTokensRefreshedCallback tokensRefreshedCallback,
        string accessToken,
        string refreshToken)
    {
        var sessionTokens = new SessionTokens
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        var tokenBytes = InteropArray.FromMemory(sessionTokens.ToByteArray());

        try
        {
            tokensRefreshedCallback.OnTokenRefreshed(tokensRefreshedCallback.State, tokenBytes);
        }
        finally
        {
            tokenBytes.Free();
        }
    }
}

using Proton.Sdk.Http;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Users;

internal readonly struct UsersApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<UserResponse> GetAuthenticatedUserAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonCoreApiSerializerContext.Default.UserResponse)
            .GetAsync("core/v4/users", cancellationToken).ConfigureAwait(false);
    }
}

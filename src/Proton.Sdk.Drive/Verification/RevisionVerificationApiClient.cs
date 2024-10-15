using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Http;

namespace Proton.Sdk.Drive.Verification;

internal readonly struct RevisionVerificationApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<VerificationInputResponse> GetVerificationInputAsync(
        ShareId shareId,
        LinkId linkId,
        RevisionId revisionId,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.VerificationInputResponse)
            .GetAsync($"shares/{shareId}/links/{linkId}/revisions/{revisionId}/verification", cancellationToken).ConfigureAwait(false);
    }
}

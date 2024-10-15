using Proton.Sdk.Drive.Folders;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Http;

namespace Proton.Sdk.Drive.Shares;

internal readonly struct SharesApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<ShareListResponse> GetSharesAsync(CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.ShareListResponse)
            .GetAsync("shares", cancellationToken).ConfigureAwait(false);
    }

    public async Task<ShareResponse> GetShareAsync(ShareId id, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.ShareResponse)
            .GetAsync($"shares/{id}", cancellationToken).ConfigureAwait(false);
    }

    public async Task<AggregateResponse<LinkActionResponse>> DeleteFromTrashAsync(
        ShareId id,
        MultipleLinkActionParameters parameters,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.AggregateResponseLinkActionResponse)
            .PostAsync(
                $"shares/{id}/trash/delete_multiple",
                parameters,
                ProtonDriveApiSerializerContext.Default.MultipleLinkActionParameters,
                cancellationToken).ConfigureAwait(false);
    }
}

using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Http;

namespace Proton.Sdk.Drive.Links;

internal readonly struct LinksApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<LinkResponse> GetLinkAsync(ShareId shareId, LinkId linkId, CancellationToken cancellationToken, byte[]? operationId = null)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.LinkResponse)
            .GetAsync($"shares/{shareId}/links/{linkId}", cancellationToken, operationId).ConfigureAwait(false);
    }

    public async Task<ApiResponse> MoveLinkAsync(ShareId shareId, LinkId linkId, MoveLinkParameters parameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.ApiResponse)
            .PutAsync($"shares/{shareId}/links/{linkId}/move", parameters, ProtonDriveApiSerializerContext.Default.MoveLinkParameters, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ApiResponse> RenameLinkAsync(ShareId shareId, LinkId linkId, RenameLinkParameters parameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.ApiResponse)
            .PutAsync($"shares/{shareId}/links/{linkId}/rename", parameters, ProtonDriveApiSerializerContext.Default.RenameLinkParameters, cancellationToken)
            .ConfigureAwait(false);
    }
}

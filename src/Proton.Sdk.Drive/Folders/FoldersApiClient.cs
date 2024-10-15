using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Http;

namespace Proton.Sdk.Drive.Folders;

internal readonly struct FoldersApiClient(HttpClient httpClient)
{
    public const int FolderChildListingPageSize = 150;

    private readonly HttpClient _httpClient = httpClient;

    public async Task<FolderChildListResponse> GetChildrenAsync(
        ShareId shareId,
        LinkId linkId,
        FolderChildListParameters parameters,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.FolderChildListResponse)
            .GetAsync(
                $"shares/{shareId}/folders/{linkId}/children?Page={parameters.PageIndex}&PageSize={parameters.PageSize ?? FolderChildListingPageSize}&ShowAll={(parameters.ShowAll ? 1 : 0)}",
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<FolderCreationResponse> CreateFolderAsync(ShareId shareId, FolderCreationParameters parameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.FolderCreationResponse)
            .PostAsync($"shares/{shareId}/folders", parameters, ProtonDriveApiSerializerContext.Default.FolderCreationParameters, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AggregateResponse<LinkActionResponse>> TrashChildrenAsync(
        ShareId shareId,
        LinkId linkId,
        MultipleLinkActionParameters parameters,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.AggregateResponseLinkActionResponse)
            .PostAsync(
                $"shares/{shareId}/folders/{linkId}/trash_multiple",
                parameters,
                ProtonDriveApiSerializerContext.Default.MultipleLinkActionParameters,
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<AggregateResponse<LinkActionResponse>> DeleteChildrenAsync(
        ShareId shareId,
        LinkId linkId,
        MultipleLinkActionParameters parameters,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.AggregateResponseLinkActionResponse)
            .PostAsync(
                $"shares/{shareId}/folders/{linkId}/delete_multiple",
                parameters,
                ProtonDriveApiSerializerContext.Default.MultipleLinkActionParameters,
                cancellationToken).ConfigureAwait(false);
    }
}

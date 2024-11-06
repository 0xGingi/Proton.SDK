using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Http;

namespace Proton.Sdk.Drive.Files;

internal readonly struct FilesApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<FileCreationApiResponse> CreateFileAsync(
        ShareId shareId,
        FileCreationParameters parameters,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.FileCreationApiResponse, ProtonDriveApiSerializerContext.Default.RevisionConflictResponse)
            .PostAsync($"shares/{shareId}/files", parameters, ProtonDriveApiSerializerContext.Default.FileCreationParameters, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BlockRequestResponse> RequestBlockUploadAsync(BlockUploadRequestParameters parameters, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.BlockRequestResponse)
            .PostAsync("blocks", parameters, ProtonDriveApiSerializerContext.Default.BlockUploadRequestParameters, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RevisionResponse> GetRevisionAsync(
        ShareId shareId,
        LinkId linkId,
        RevisionId revisionId,
        int fromBlockIndex,
        int pageSize,
        bool noBlockUrls,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.RevisionResponse)
            .GetAsync(
                $"shares/{shareId}/files/{linkId}/revisions/{revisionId}?FromBlockIndex={fromBlockIndex}&PageSize={pageSize}&NoBlockUrls={(noBlockUrls ? 1 : 0)}",
                cancellationToken).ConfigureAwait(false);
    }

    public async Task<RevisionListResponse> GetRevisionsAsync(ShareId shareId, LinkId linkId, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.RevisionListResponse)
            .GetAsync($"shares/{shareId}/files/{linkId}/revisions", cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiResponse> DeleteRevisionAsync(ShareId shareId, LinkId linkId, RevisionId revisionId, CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.ApiResponse)
            .DeleteAsync($"shares/{shareId}/files/{linkId}/revisions/{revisionId}", cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApiResponse> UpdateRevisionAsync(
        ShareId shareId,
        LinkId linkId,
        RevisionId revisionId,
        RevisionUpdateParameters parameters,
        CancellationToken cancellationToken)
    {
        return await _httpClient
            .Expecting(ProtonDriveApiSerializerContext.Default.ApiResponse)
            .PutAsync(
                $"shares/{shareId}/files/{linkId}/revisions/{revisionId}",
                parameters,
                ProtonDriveApiSerializerContext.Default.RevisionUpdateParameters,
                cancellationToken).ConfigureAwait(false);
    }
}

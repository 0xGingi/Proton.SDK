using System.Net.Http.Headers;
using System.Net.Mime;
using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Http;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Storage;

internal readonly struct StorageApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<ApiResponse> UploadBlobAsync(string url, Stream dataPacketStream, CancellationToken cancellationToken)
    {
        using var blobContent = new StreamContent(dataPacketStream);
        blobContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "Block", FileName = "blob" };
        blobContent.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Octet);

        var multipartContent = new MultipartFormDataContent("-----------------------------" + Guid.NewGuid().ToString("N")) { blobContent };

        using (multipartContent)
        {
            return await _httpClient
                .Expecting(ProtonDriveApiSerializerContext.Default.ApiResponse)
                .PostAsync(url, multipartContent, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<Stream> GetBlobStreamAsync(string url, CancellationToken cancellationToken)
    {
        var blobResponse = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        await blobResponse.EnsureApiSuccessAsync(ProtonCoreApiSerializerContext.Default.ApiResponse, cancellationToken).ConfigureAwait(false);

        return await blobResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }
}

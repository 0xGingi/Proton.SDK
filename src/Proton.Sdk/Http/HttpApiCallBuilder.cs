using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Proton.Sdk.Http;

internal readonly struct HttpApiCallBuilder<TResponseBody>
{
    private readonly HttpClient _httpClient;
    private readonly JsonTypeInfo<TResponseBody> _responseTypeInfo;

    internal HttpApiCallBuilder(HttpClient httpClient, JsonTypeInfo<TResponseBody> responseTypeInfo)
    {
        _httpClient = httpClient;
        _responseTypeInfo = responseTypeInfo;
    }

    public async ValueTask<TResponseBody> GetAsync(string requestUri, CancellationToken cancellationToken)
    {
        using var requestMessage = HttpRequestMessageFactory.Create(HttpMethod.Get, requestUri);
        return await SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TResponseBody> GetAsync(string requestUri, string sessionId, string accessToken, CancellationToken cancellationToken)
    {
        using var requestMessage = HttpRequestMessageFactory.Create(HttpMethod.Get, requestUri, sessionId, accessToken);
        return await SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TResponseBody> PostAsync<TRequestBody>(
        string requestUri,
        TRequestBody body,
        JsonTypeInfo<TRequestBody> bodyTypeInfo,
        CancellationToken cancellationToken)
    {
        using var requestMessage = HttpRequestMessageFactory.Create(HttpMethod.Post, requestUri, body, bodyTypeInfo);
        return await SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TResponseBody> PostAsync<TRequestBody>(
        string requestUri,
        string accessToken,
        TRequestBody body,
        JsonTypeInfo<TRequestBody> bodyTypeInfo,
        CancellationToken cancellationToken)
    {
        using var requestMessage = HttpRequestMessageFactory.Create(HttpMethod.Post, requestUri, accessToken, body, bodyTypeInfo);
        return await SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TResponseBody> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken)
    {
        using var requestMessage = HttpRequestMessageFactory.Create(HttpMethod.Post, requestUri, content);
        return await SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TResponseBody> PutAsync<TRequestBody>(
        string requestUri,
        TRequestBody body,
        JsonTypeInfo<TRequestBody> bodyTypeInfo,
        CancellationToken cancellationToken)
    {
        using var requestMessage = HttpRequestMessageFactory.Create(HttpMethod.Put, requestUri, body, bodyTypeInfo);
        return await SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TResponseBody> DeleteAsync(string requestUri, CancellationToken cancellationToken)
    {
        using var requestMessage = HttpRequestMessageFactory.Create(HttpMethod.Delete, requestUri);
        return await SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<TResponseBody> DeleteAsync(string requestUri, string sessionId, string accessToken, CancellationToken cancellationToken)
    {
        using var requestMessage = HttpRequestMessageFactory.Create(HttpMethod.Delete, requestUri, sessionId, accessToken);
        return await SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<TResponseBody> SendAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        var responseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        await responseMessage.EnsureApiSuccessAsync(cancellationToken).ConfigureAwait(false);

        return await responseMessage.Content.ReadFromJsonAsync(_responseTypeInfo, cancellationToken)
            .ConfigureAwait(false) ?? throw new JsonException();
    }
}

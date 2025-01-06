namespace Proton.Sdk.CExports;

public sealed class ResponsePassingHttpHandler(Action<byte[], HttpMethod, Uri?, string, string> passResponse) : DelegatingHandler
{
    internal static DelegatingHandler Create(InteropRequestResponseBodyCallback requestResponseBodyCallback)
    {
        return new ResponsePassingHttpHandler(
            (operationId, method, url, requestBody, responseBody) =>
                requestResponseBodyCallback.ResponseReceived(operationId, method, url, requestBody, responseBody));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var shouldPassResponse = request.Options.TryGetValue(new HttpRequestOptionsKey<byte[]>("ShouldBePassedWithOperationId"), out var operationId);
        var message = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (operationId is null || !shouldPassResponse)
        {
            return message;
        }

        var requestBody = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)
            : string.Empty;
        var responseBody = await message.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        passResponse(
            operationId,
            request.Method,
            request.RequestUri,
            requestBody,
            responseBody);
        return message;
    }
}

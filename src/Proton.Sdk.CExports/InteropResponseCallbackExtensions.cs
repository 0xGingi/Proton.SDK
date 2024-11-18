using Google.Protobuf;
using static System.Net.Http.HttpMethod;

namespace Proton.Sdk.CExports;

internal static class InteropResponseCallbackExtensions
{
    internal static unsafe void ResponseReceived(
        this InteropRequestResponseBodyCallback requestResponseBodyCallback,
        byte[] operationId,
        HttpMethod method,
        Uri? url,
        string requestBody,
        string responseBody)
    {
        var responseBodyResponse = new RequestResponseBodyResponse
        {
            OperationId = OperationIdentifier.Parser.ParseFrom(operationId),
            Method = FromHttpMethod(method),
            Url = url?.AbsoluteUri ?? string.Empty,
            RequestBody = requestBody,
            ResponseBody = responseBody,
        };
        requestResponseBodyCallback.OnResponseBodyReceived(requestResponseBodyCallback.State, InteropArray.FromMemory(responseBodyResponse.ToByteArray()));
    }

    private static RequestMethod FromHttpMethod(HttpMethod method)
    {
        if (method == Get)
        {
            return RequestMethod.Get;
        }

        if (method == Post)
        {
            return RequestMethod.Post;
        }

        if (method == Put)
        {
            return RequestMethod.Put;
        }

        if (method == Delete)
        {
            return RequestMethod.Delete;
        }

        return RequestMethod.Invalid;
    }
}

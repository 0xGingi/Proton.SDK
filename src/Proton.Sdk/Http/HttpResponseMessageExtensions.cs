using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Http;

internal static class HttpResponseMessageExtensions
{
    public static async Task EnsureApiSuccessAsync(this HttpResponseMessage responseMessage, CancellationToken cancellationToken)
    {
        if (responseMessage.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity or HttpStatusCode.TooManyRequests)
        {
            var response = await responseMessage.Content.ReadFromJsonAsync(ProtonCoreApiSerializerContext.Default.ApiResponse, cancellationToken)
                .ConfigureAwait(false) ?? throw new JsonException();

            throw new ProtonApiException($"{response.Code}: {response.ErrorMessage}");
        }

        responseMessage.EnsureSuccessStatusCode();
    }
}

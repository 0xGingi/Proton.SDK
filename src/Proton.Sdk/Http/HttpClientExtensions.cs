using System.Text.Json.Serialization.Metadata;

namespace Proton.Sdk.Http;

internal static class HttpClientExtensions
{
    public static HttpApiCallBuilder<T> Expecting<T>(this HttpClient httpClient, JsonTypeInfo<T> responseTypeInfo)
    {
        return new HttpApiCallBuilder<T>(httpClient, responseTypeInfo);
    }
}

using System.Net.Http.Headers;

namespace Proton.Sdk.Http;

internal static class HttpRequestHeadersExtensions
{
    private const string DefaultLanguage = "en-US,en";
    private const string ContentType = "application/vnd.protonmail.api+json";

    public static void AddApiRequestHeaders(this HttpRequestHeaders headerCollection)
    {
        headerCollection.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType));
    }
}

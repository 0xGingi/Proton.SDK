using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Proton.Sdk.Serialization;

internal static class JsonSerializerOptionsExtensions
{
    internal static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
    {
        return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
    }
}

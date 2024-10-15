using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk.Drive.Links;

internal readonly struct FolderProperties
{
    [JsonPropertyName("NodeHashKey")]
    public required PgpArmoredMessage HashKey { get; init; }
}

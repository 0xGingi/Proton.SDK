using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Drive.Links;

namespace Proton.Sdk.Drive.Folders;

internal sealed class FolderCreationParameters : NodeCreationParameters
{
    [JsonPropertyName("NodeHashKey")]
    public required PgpArmoredMessage HashKey { get; init; }
}

using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class CommonExtendedAttributes
{
    public long? Size { get; init; }

    [JsonPropertyName("ModificationTime")]
    public DateTime? LastModificationTime { get; init; }

    public IReadOnlyList<int>? BlockSizes { get; init; }
}

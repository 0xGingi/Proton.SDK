using System.Text.Json.Serialization;
using Proton.Sdk.Drive.Serialization;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Volumes;

[JsonConverter(typeof(EventJsonConverter))]
internal abstract class EventDto
{
    [JsonPropertyName("EventID")]
    public required string Id { get; init; }

    [JsonPropertyName("EventType")]
    public required VolumeEventType Type { get; init; }

    [JsonPropertyName("CreationTime")]
    [JsonConverter(typeof(EpochSecondsJsonConverter))]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("FromContextShareID")]
    public string? OriginContextShareId { get; init; }
}

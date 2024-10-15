using System.Text.Json.Serialization;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Volumes;

internal sealed class VolumeEventListResponse : ApiResponse
{
    [JsonPropertyName("EventID")]
    public required string LastEventId { get; init; }

    public required IReadOnlyList<EventDto> Events { get; init; }

    [JsonPropertyName("More")]
    [JsonConverter(typeof(BooleanToIntegerJsonConverter))]
    public required bool MoreEntriesExist { get; init; }

    [JsonPropertyName("Refresh")]
    [JsonConverter(typeof(BooleanToIntegerJsonConverter))]
    public required bool RequiresRefresh { get; init; }
}

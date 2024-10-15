using System.Text.Json.Serialization;

namespace Proton.Sdk.Events;

internal sealed class LatestEventResponse : ApiResponse
{
    [JsonPropertyName("EventID")]
    public required string EventId { get; init; }
}

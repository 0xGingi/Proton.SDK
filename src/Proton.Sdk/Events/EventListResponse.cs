﻿using System.Text.Json.Serialization;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Events;

internal sealed class EventListResponse : ApiResponse
{
    [JsonPropertyName("EventID")]
    public required string LastEventId { get; init; }

    [JsonPropertyName("More")]
    [JsonConverter(typeof(BooleanToIntegerJsonConverter))]
    public required bool MoreEntriesExist { get; init; }

    [JsonPropertyName("Refresh")]
    public EventsRefreshMask RefreshMask { get; init; }

    public IReadOnlyList<AddressEvent>? AddressEvents { get; init; }

    public long? UsedSpace { get; init; }

    public long? UsedDriveSpace { get; init; }
}

﻿using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Devices;

internal sealed record DeviceShare
{
    [JsonPropertyName("ShareID")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("LinkID")]
    public string LinkId { get; init; } = string.Empty;

    /// <summary>
    /// Device name
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

﻿using System.Text.Json.Serialization;

namespace Proton.Sdk.Drive.Files;

internal sealed class ThumbnailDto
{
    [JsonPropertyName("ThumbnailID")]
    public string? Id { get; init; }

    public required ThumbnailType Type { get; init; }

    [JsonPropertyName("Hash")]
    public required ReadOnlyMemory<byte> HashDigest { get; init; }

    public required int Size { get; init; }
}

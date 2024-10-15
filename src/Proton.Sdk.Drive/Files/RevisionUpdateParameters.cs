﻿using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionUpdateParameters
{
    public required PgpArmoredSignature ManifestSignature { get; init; }

    [JsonPropertyName("SignatureAddress")]
    public required string SignatureEmailAddress { get; init; }

    [JsonPropertyName("XAttr")]
    public PgpArmoredMessage? ExtendedAttributes { get; init; }
}

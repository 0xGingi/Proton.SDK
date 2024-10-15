﻿using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk.Drive.Files;

internal sealed class BlockCreationParameters
{
    public required int Index { get; init; }
    public required int Size { get; init; }

    [JsonPropertyName("EncSignature")]
    public required PgpArmoredMessage EncryptedSignature { get; init; }

    [JsonPropertyName("Hash")]
    public required ReadOnlyMemory<byte> HashDigest { get; init; }

    [JsonPropertyName("Verifier")]
    public required BlockVerifierOutput VerifierOutput { get; init; }
}

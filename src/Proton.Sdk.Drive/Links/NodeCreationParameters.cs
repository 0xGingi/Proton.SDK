﻿using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Links;

internal abstract class NodeCreationParameters
{
    public required PgpArmoredMessage Name { get; init; }

    [JsonPropertyName("Hash")]
    [JsonConverter(typeof(ForgivingBytesToHexJsonConverter))]
    public required ReadOnlyMemory<byte> NameHashDigest { get; init; }

    [JsonPropertyName("ParentLinkID")]
    public required string ParentLinkId { get; init; }

    [JsonPropertyName("NodePassphrase")]
    public required PgpArmoredMessage Passphrase { get; init; }

    [JsonPropertyName("NodePassphraseSignature")]
    public required PgpArmoredSignature PassphraseSignature { get; init; }

    [JsonPropertyName("SignatureAddress")]
    public required string SignatureEmailAddress { get; init; }

    [JsonPropertyName("NodeKey")]
    public required PgpArmoredPrivateKey Key { get; init; }
}

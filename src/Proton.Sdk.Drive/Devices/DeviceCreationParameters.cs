using System.Text.Json.Serialization;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Serialization;

namespace Proton.Sdk.Drive.Devices;

internal sealed class DeviceCreationParameters
{
    public required DeviceProperties Device { get; init; }

    public required ShareProperties Share { get; init; }

    public required LinkProperties Link { get; init; }

    public sealed class DeviceProperties
    {
        [JsonPropertyName("VolumeID")]
        public required string VolumeId { get; init; }

        [JsonPropertyName("Type")]
        public required DevicePlatform Platform { get; init; }

        [JsonPropertyName("SyncState")]
        [JsonConverter(typeof(BooleanToIntegerJsonConverter))]
        public required bool SynchronizationIsEnabled { get; init; }
    }

    public sealed class ShareProperties
    {
        [JsonPropertyName("AddressID")]
        public required string AddressId { get; init; }

        public required PgpArmoredPrivateKey Key { get; init; }

        public required PgpArmoredMessage Passphrase { get; init; }

        [JsonPropertyName("PassphraseSignature")]
        public PgpArmoredSignature? PassphraseSignature { get; init; }
    }

    public sealed class LinkProperties
    {
        public required string Name { get; init; }

        [JsonPropertyName("NodeKey")]
        public required PgpArmoredPrivateKey Key { get; init; }

        [JsonPropertyName("NodePassphrase")]
        public required PgpArmoredMessage Passphrase { get; init; }

        [JsonPropertyName("NodePassphraseSignature")]
        public required PgpArmoredSignature PassphraseSignature { get; init; }

        [JsonPropertyName("NodeHashKey")]
        public required ReadOnlyMemory<byte> HashKey { get; init; }
    }
}

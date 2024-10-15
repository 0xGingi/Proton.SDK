using System.Text.Json;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Cryptography;
using Proton.Sdk.Serialization;

namespace Proton.Sdk;

internal static class ProtonApiDefaults
{
    public static TimeSpan DefaultPollingInterval => TimeSpan.FromSeconds(10);

    public static Uri BaseUrl { get; } = new("https://drive-api.proton.me/");

    public static JsonSerializerOptions GetSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            Converters =
            {
                new PgpArmoredBlockJsonConverter<PgpArmoredMessage>(PgpBlockType.Message, bytes => new PgpArmoredMessage(bytes)),
                new PgpArmoredBlockJsonConverter<PgpArmoredSignature>(PgpBlockType.Signature, bytes => new PgpArmoredSignature(bytes)),
                new PgpArmoredBlockJsonConverter<PgpArmoredPublicKey>(PgpBlockType.PublicKey, bytes => new PgpArmoredPublicKey(bytes)),
                new PgpArmoredBlockJsonConverter<PgpArmoredPrivateKey>(PgpBlockType.PrivateKey, bytes => new PgpArmoredPrivateKey(bytes)),
            },
#if DEBUG
            WriteIndented = true,
#endif
        };
    }
}

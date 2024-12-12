using System.Security.Cryptography;
using Proton.Cryptography.Pgp;
using Proton.Sdk.Drive.Volumes;

namespace Proton.Sdk.Drive;

public sealed class Volume(VolumeId id, ShareId rootShareId, VolumeState state, long? maxSpace)
{
    internal Volume(VolumeDto dto)
        : this(new VolumeId(dto.Id), new ShareId(dto.Root.ShareId), dto.State, dto.MaxSpace)
    {
    }

    public VolumeId Id { get; } = id;

    public ShareId RootShareId { get; } = rootShareId;

    public VolumeState State { get; } = state;

    public long? MaxSpace { get; } = maxSpace;

    internal static async Task<Volume[]> GetAllAsync(VolumesApiClient client, CancellationToken cancellationToken)
    {
        var volumeListResponse = await client.GetVolumesAsync(cancellationToken).ConfigureAwait(false);

        return volumeListResponse.Volumes.Select(dto => new Volume(dto)).ToArray();
    }

    internal static async Task<Volume> CreateAsync(ProtonDriveClient client, CancellationToken cancellationToken)
    {
        var defaultAddress = await client.Account.GetDefaultAddressAsync(cancellationToken).ConfigureAwait(false);

        using var addressKey = await client.Account.GetAddressPrimaryKeyAsync(defaultAddress.Id, cancellationToken).ConfigureAwait(false);

        var parameters = GetCreationParameters(defaultAddress.Id, addressKey);

        var response = await client.VolumesApi.CreateVolumeAsync(parameters, cancellationToken).ConfigureAwait(false);

        return new Volume(response.Volume);
    }

    private static VolumeCreationParameters GetCreationParameters(AddressId addressId, PgpPrivateKey addressKey)
    {
        const string folderName = "root";

        Span<byte> shareKeyPassphraseBuffer = stackalloc byte[Share.PassphraseMaxUtf8Length];
        var shareKeyPassphrase = Share.GeneratePassphrase(shareKeyPassphraseBuffer);
        var shareKey = PgpPrivateKey.Generate("Drive key", "no-reply@proton.me", KeyGenerationAlgorithm.Default);
        using var lockedShareKey = shareKey.Lock(shareKeyPassphrase);

        var encryptedShareKeyPassphrase = addressKey.EncryptAndSign(shareKeyPassphrase, addressKey, out var shareKeyPassphraseSignature);

        Span<byte> folderKeyPassphraseBuffer = stackalloc byte[Share.PassphraseMaxUtf8Length];
        var folderKeyPassphrase = Share.GeneratePassphrase(folderKeyPassphraseBuffer);
        var folderKey = PgpPrivateKey.Generate("Drive key", "no-reply@proton.me", KeyGenerationAlgorithm.Default);
        using var lockedFolderKey = folderKey.Lock(folderKeyPassphrase);

        var encryptedFolderKeyPassphrase = shareKey.EncryptAndSign(folderKeyPassphrase, addressKey, out var folderKeyPassphraseSignature);

        Span<byte> folderHashKey = stackalloc byte[32];
        RandomNumberGenerator.Fill(folderHashKey);

        return new VolumeCreationParameters
        {
            AddressId = addressId.Value,
            ShareKey = lockedShareKey.ToBytes(),
            ShareKeyPassphrase = encryptedShareKeyPassphrase,
            ShareKeyPassphraseSignature = shareKeyPassphraseSignature,
            FolderName = shareKey.EncryptAndSignText(folderName, addressKey),
            FolderKey = lockedFolderKey.ToBytes(),
            FolderKeyPassphrase = encryptedFolderKeyPassphrase,
            FolderKeyPassphraseSignature = folderKeyPassphraseSignature,
            FolderHashKey = folderKey.EncryptAndSign(folderHashKey, addressKey),
        };
    }
}

using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropRevision(
    InteropArray volumeId,
    InteropArray fileId,
    InteropArray id,
    byte state,
    long size,
    long quotaConsumption,
    long creationTime,
    InteropArray manifestSignature,
    InteropArray signatureEmailAddress,
    InteropArray samplesSha256Digests)
{
    public readonly InteropArray VolumeId = volumeId;
    public readonly InteropArray FileId = fileId;
    public readonly InteropArray Id = id;
    public readonly byte State = state;
    public readonly long Size = size;
    public readonly long QuotaConsumption = quotaConsumption;
    public readonly long CreationTime = creationTime;

    public readonly InteropArray ManifestSignature = manifestSignature;
    public readonly InteropArray SignatureEmailAddress = signatureEmailAddress;
    public readonly InteropArray SamplesSha256Digests = samplesSha256Digests;

    public static InteropRevision FromManaged(Revision revision)
    {
        return new InteropRevision(
            InteropArray.Utf8FromString(revision.VolumeId.ToString()),
            InteropArray.Utf8FromString(revision.FileId.ToString()),
            InteropArray.Utf8FromString(revision.Id.ToString()),
            (byte)revision.State,
            revision.Size ?? -1,
            revision.QuotaConsumption,
            revision.CreationTime == default ? DateTimeOffset.Now.ToUnixTimeSeconds() : new DateTimeOffset(revision.CreationTime).ToUnixTimeSeconds(),
            InteropArray.FromMemory(revision.ManifestSignature ?? ReadOnlyMemory<byte>.Empty),
            InteropArray.Utf8FromString(revision.SignatureEmailAddress ?? string.Empty),
            InteropArray.Null);
    }
}

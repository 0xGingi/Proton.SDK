using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropFileNode(
    InteropArray volumeId,
    InteropArray id,
    InteropArray parentId,
    InteropArray name,
    byte state,
    InteropArray nameHashDigest)
{
    public readonly InteropArray VolumeId = volumeId;
    public readonly InteropArray Id = id;
    public readonly InteropArray ParentId = parentId;
    public readonly InteropArray Name = name;
    public readonly byte State = state;
    public readonly InteropArray NameHashDigest = nameHashDigest;

    public static InteropFileNode FromManaged(FileNode file)
    {
        return new InteropFileNode(
            InteropArray.Utf8FromString(file.VolumeId.ToString()),
            InteropArray.Utf8FromString(file.Id.ToString()),
            InteropArray.Utf8FromString(file.ParentId?.ToString() ?? string.Empty),
            InteropArray.Utf8FromString(file.Name),
            (byte)file.State,
            InteropArray.FromMemory(file.NameHashDigest));
    }
}

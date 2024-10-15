using System.Runtime.InteropServices;

namespace Proton.Sdk.Drive.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropFileRevisionPair(InteropFileNode file, InteropRevision revision)
{
    public readonly InteropFileNode File = file;
    public readonly InteropRevision Revision = revision;

    public static InteropFileRevisionPair FromManaged(FileNode file, Revision revision)
    {
        return new InteropFileRevisionPair(InteropFileNode.FromManaged(file), InteropRevision.FromManaged(revision));
    }
}

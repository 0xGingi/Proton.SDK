using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropExternalWriter
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, byte*, nuint, InteropExternalAsyncCallback, void> Write;

    public Stream ToStream()
    {
        return new InteropOutputStream(this);
    }
}

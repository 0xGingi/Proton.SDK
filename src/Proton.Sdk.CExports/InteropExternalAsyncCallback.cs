using System.Runtime.InteropServices;

namespace Proton.Sdk.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropExternalAsyncCallback(
    nint continuationHandle,
    delegate* unmanaged[Cdecl]<nint, void> onSuccess,
    delegate* unmanaged[Cdecl]<nint, void> onFailure)
{
    public readonly nint ContinuationHandle = continuationHandle;
    public readonly delegate* unmanaged[Cdecl]<nint, void> OnSuccess = onSuccess;
    public readonly delegate* unmanaged[Cdecl]<nint, void> OnFailure = onFailure;
}

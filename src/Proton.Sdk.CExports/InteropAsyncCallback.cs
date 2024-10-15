using System.Runtime.InteropServices;

namespace Proton.Sdk.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropAsyncCallback
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, void> OnSuccess;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropSdkError, void> OnFailure;
    public readonly nint CancellationTokenSourceHandle;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropAsyncCallbackNoCancellation
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, void> OnSuccess;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropSdkError, void> OnFailure;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropAsyncCallback<T>
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, T, void> OnSuccess;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropSdkError, void> OnFailure;
    public readonly nint CancellationTokenSourceHandle;
}

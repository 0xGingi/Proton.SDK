using System.Runtime.InteropServices;

namespace Proton.Sdk.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropAsyncCallback
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropArray, void> OnSuccess;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropArray, void> OnFailure;
    public readonly nint CancellationTokenSourceHandle;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropProgressCallback
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropArray, void> OnProgress;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropRequestResponseBodyCallback
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropArray, void> OnResponseBodyReceived;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropTokensRefreshedCallback
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropArray, void> OnTokenRefreshed;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropAsyncCallbackWithProgress
{
    public readonly InteropAsyncCallback AsyncCallback;
    public readonly InteropProgressCallback ProgressCallback;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropSecretRequestedCallback
{
    public readonly void* State;
    public readonly delegate* unmanaged[Cdecl]<void*, InteropArray, bool> OnSecretRequested;
}

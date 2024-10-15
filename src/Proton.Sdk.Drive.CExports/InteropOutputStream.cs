using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal sealed class InteropOutputStream(InteropExternalWriter externalWriter) : Stream
{
    private readonly InteropExternalWriter _externalWriter = externalWriter;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
        // Nothing to do
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        using var memoryOwner = buffer.Pin();

        var taskCompletionSource = new TaskCompletionSource();
        var taskCompletionSourceHandle = GCHandle.Alloc(taskCompletionSource);

        WriteWithCallback(memoryOwner, (nuint)buffer.Length, GCHandle.ToIntPtr(taskCompletionSourceHandle));

        await taskCompletionSource.Task.ConfigureAwait(false);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnWriteSuccess(nint taskCompletionSourceHandle)
    {
        var taskCompletionSource = GCHandle.FromIntPtr(taskCompletionSourceHandle).Target as TaskCompletionSource;

        taskCompletionSource?.SetResult();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnWriteError(nint taskCompletionSourceHandle)
    {
        var taskCompletionSource = GCHandle.FromIntPtr(taskCompletionSourceHandle).Target as TaskCompletionSource;

        taskCompletionSource?.SetException(new IOException("Error while writing"));
    }

    private unsafe void WriteWithCallback(MemoryHandle bufferHandle, nuint bufferLength, nint taskCompletionSourceHandle)
    {
        var callback = new InteropExternalAsyncCallback(taskCompletionSourceHandle, &OnWriteSuccess, &OnWriteError);

        _externalWriter.Write(_externalWriter.State, (byte*)bufferHandle.Pointer, bufferLength, callback);
    }
}

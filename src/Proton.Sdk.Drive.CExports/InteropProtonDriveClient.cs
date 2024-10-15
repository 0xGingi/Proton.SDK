using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropProtonDriveClient
{
    internal static bool TryGetFromHandle(nint handle, [MaybeNullWhen(false)] out ProtonDriveClient client)
    {
        var gcHandle = GCHandle.FromIntPtr(handle);

        client = gcHandle.Target as ProtonDriveClient;

        return client is not null;
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_create", CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe int Create(nint sessionHandle, nint* clientHandle)
    {
        try
        {
            if (!InteropProtonApiSession.TryGetFromHandle(sessionHandle, out var session))
            {
                return -1;
            }

            var client = new ProtonDriveClient(session);

            *clientHandle = GCHandle.ToIntPtr(GCHandle.Alloc(client));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_create_file", CallConvs = [typeof(CallConvCdecl)])]
    private static int CreateFile(
        nint clientHandle,
        InteropShareForCommand share,
        InteropNodeIdentity parentFolder,
        InteropArray name,
        InteropArray mediaType,
        InteropAsyncCallback<InteropFileRevisionPair> callback)
    {
        try
        {
            if (!TryGetFromHandle(clientHandle, out var client))
            {
                return -1;
            }

            callback.InvokeFor(ct => CreateFileAsync(client, share.ToManaged(), parentFolder.ToManaged(), name.Utf8ToString(), mediaType.Utf8ToString(), ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_open_revision_for_reading", CallConvs = [typeof(CallConvCdecl)])]
    private static int OpenRevisionForReading(
        nint clientHandle,
        InteropArray shareId,
        InteropNodeIdentity file,
        InteropRevisionForTransfer revision,
        InteropAsyncCallback<nint> callback)
    {
        try
        {
            if (!TryGetFromHandle(clientHandle, out var client))
            {
                return -1;
            }

            callback.InvokeFor(
                ct => OpenRevisionForReadingAsync(client, new ShareId(shareId.Utf8ToString()), file.ToManaged(), revision.ToManaged(), ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_open_revision_for_writing", CallConvs = [typeof(CallConvCdecl)])]
    private static int OpenRevisionForWriting(
        nint clientHandle,
        InteropShareForCommand share,
        InteropNodeIdentity file,
        InteropRevisionForTransfer revision,
        InteropAsyncCallback<nint> callback)
    {
        try
        {
            if (!TryGetFromHandle(clientHandle, out var client))
            {
                return -1;
            }

            callback.InvokeFor(
                ct => OpenRevisionForWritingAsync(client, share.ToManaged(), file.ToManaged(), revision.ToManaged(), ct));

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "drive_client_free", CallConvs = [typeof(CallConvCdecl)])]
    private static void Free(nint handle)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr(handle);

            gcHandle.Free();
        }
        catch
        {
            // Ignore
        }
    }

    private static async ValueTask<Result<InteropFileRevisionPair, SdkError>> CreateFileAsync(
        ProtonDriveClient client,
        IShareForCommand share,
        INodeIdentity parentFolder,
        string name,
        string mediaType,
        CancellationToken cancellationToken)
    {
        try
        {
            var (file, revision) = await client.CreateFileAsync(share, parentFolder, name, mediaType, cancellationToken).ConfigureAwait(false);

            return InteropFileRevisionPair.FromManaged(file, revision);
        }
        catch (Exception ex)
        {
            return new SdkError(-1, ex.Message);
        }
    }

    private static async ValueTask<Result<nint, SdkError>> OpenRevisionForReadingAsync(
        ProtonDriveClient client,
        ShareId shareId,
        INodeIdentity file,
        IRevisionForTransfer revision,
        CancellationToken cancellationToken)
    {
        try
        {
            var revisionReader = await client.OpenRevisionForReadingAsync(shareId, file, revision, cancellationToken).ConfigureAwait(false);

            return GCHandle.ToIntPtr(GCHandle.Alloc(revisionReader));
        }
        catch (Exception ex)
        {
            return new SdkError(-1, ex.Message);
        }
    }

    private static async ValueTask<Result<nint, SdkError>> OpenRevisionForWritingAsync(
        ProtonDriveClient client,
        IShareForCommand share,
        INodeIdentity file,
        IRevisionForTransfer revision,
        CancellationToken cancellationToken)
    {
        try
        {
            var revisionWriter = await client.OpenRevisionForWritingAsync(share, file, revision, cancellationToken).ConfigureAwait(false);

            return GCHandle.ToIntPtr(GCHandle.Alloc(revisionWriter));
        }
        catch (Exception ex)
        {
            return new SdkError(-1, ex.Message);
        }
    }
}

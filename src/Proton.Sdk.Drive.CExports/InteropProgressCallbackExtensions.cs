using Google.Protobuf;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

internal static class InteropProgressCallbackExtensions
{
    internal static unsafe void UpdateProgress(this InteropProgressCallback progressCallback, long completed, long total)
    {
        var progressUpdate = new ProgressUpdate
        {
            BytesCompleted = completed,
            BytesInTotal = total,
        };

        var messageBytes = InteropArray.FromMemory(progressUpdate.ToByteArray());

        try
        {
            progressCallback.OnProgress(progressCallback.State, messageBytes);
        }
        finally
        {
            messageBytes.Free();
        }
    }
}

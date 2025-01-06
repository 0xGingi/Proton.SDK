namespace Proton.Sdk.Drive;

public static class RevisionReaderExtensions
{
    public static async Task<VerificationStatus> ReadAsync(this RevisionReader reader, string targetFilePath, Action<long, long> onProgress, CancellationToken cancellationToken)
    {
        FileStream fileStream;
        try
        {
            fileStream = File.Open(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        }
        catch
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }

        await using (fileStream)
        {
            return await reader.ReadAsync(fileStream, onProgress, cancellationToken).ConfigureAwait(false);
        }
    }
}

namespace Proton.Sdk.Drive;

public static class RevisionReaderExtensions
{
    public static async Task<VerificationStatus> ReadAsync(this RevisionReader reader, string targetFilePath, Action<long, long> onProgress, CancellationToken cancellationToken)
    {
        var fileStream = File.Open(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);

        await using (fileStream)
        {
            return await reader.ReadAsync(fileStream, onProgress, cancellationToken).ConfigureAwait(false);
        }
    }
}

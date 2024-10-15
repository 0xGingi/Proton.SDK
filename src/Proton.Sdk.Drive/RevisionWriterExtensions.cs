namespace Proton.Sdk.Drive;

public static class RevisionWriterExtensions
{
    public static Task WriteAsync(
        this RevisionWriter revisionWriter,
        Stream contentStream,
        DateTimeOffset? lastModificationTime,
        CancellationToken cancellationToken)
    {
        return revisionWriter.WriteAsync(contentStream, [], lastModificationTime, cancellationToken);
    }

    public static Task WriteAsync(
        this RevisionWriter revisionWriter,
        Stream contentStream,
        DateTime lastModificationTime,
        CancellationToken cancellationToken)
    {
        return revisionWriter.WriteAsync(contentStream, [], new DateTimeOffset(lastModificationTime), cancellationToken);
    }

    public static Task WriteAsync(
        this RevisionWriter revisionWriter,
        Stream contentStream,
        IEnumerable<FileSample> samples,
        DateTime lastModificationTime,
        CancellationToken cancellationToken)
    {
        return revisionWriter.WriteAsync(contentStream, samples, new DateTimeOffset(lastModificationTime), cancellationToken);
    }

    public static async Task WriteAsync(
        this RevisionWriter writer,
        string targetFilePath,
        DateTime lastModificationTime,
        CancellationToken cancellationToken)
    {
        var fileStream = File.Open(targetFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        await using (fileStream)
        {
            await WriteAsync(writer, fileStream, lastModificationTime, cancellationToken).ConfigureAwait(false);
        }
    }
}

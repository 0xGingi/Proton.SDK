namespace Proton.Sdk.Drive;

public sealed class FileContentsDecryptionException : Exception
{
    public FileContentsDecryptionException()
    {
    }

    public FileContentsDecryptionException(string message)
        : base(message)
    {
    }

    public FileContentsDecryptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public FileContentsDecryptionException(Exception innerException)
        : this("Failed to decrypt file contents", innerException)
    {
    }
}

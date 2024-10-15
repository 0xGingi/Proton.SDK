namespace Proton.Sdk.Drive;

public sealed class FileSample(FileSampleType type, ArraySegment<byte> content)
{
    public FileSampleType Type { get; } = type;
    public ArraySegment<byte> Content { get; } = content;
}

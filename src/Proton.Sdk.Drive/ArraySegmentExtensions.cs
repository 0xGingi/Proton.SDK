namespace Proton.Sdk.Drive;

internal static class ArraySegmentExtensions
{
    public static Stream AsStream(this ArraySegment<byte> arraySegment)
    {
        return arraySegment.Array is not null ? new MemoryStream(arraySegment.Array, arraySegment.Offset, arraySegment.Count) : Stream.Null;
    }
}

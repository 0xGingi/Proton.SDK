// using CommunityToolkit.HighPerformance;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Proton.Sdk.Drive.Files;

namespace Proton.Sdk.Drive;

public static class ByteStringExtensions
{
    internal static ByteString FromMemory(ReadOnlyMemory<byte>? memory)
    {
        return memory is not null ? UnsafeByteOperations.UnsafeWrap(memory.Value) : ByteString.Empty;
    }
}

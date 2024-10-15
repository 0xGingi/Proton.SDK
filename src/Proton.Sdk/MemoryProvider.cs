using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.HighPerformance.Buffers;

namespace Proton.Sdk;
internal static class MemoryProvider
{
    private const int MaxStackBufferSize = 0x100;

    public static bool GetHeapMemoryIfTooLargeForStack(int size, [MaybeNullWhen(false)] out IMemoryOwner<byte> heapMemoryOwner)
    {
        if (size <= MaxStackBufferSize)
        {
            heapMemoryOwner = null;
            return false;
        }

        heapMemoryOwner = MemoryOwner<byte>.Allocate(size);
        return true;
    }
}

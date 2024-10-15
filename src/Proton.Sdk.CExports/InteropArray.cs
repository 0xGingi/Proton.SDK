using System.Runtime.InteropServices;
using System.Text;

namespace Proton.Sdk.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct InteropArray(byte* bytes, nint length)
{
    private readonly byte* _bytes = bytes;
    private readonly nint _length = length;

    public static InteropArray Null => default;

    public bool IsNullOrEmpty => _bytes is null || _length == 0;

    public static InteropArray FromMemory(ReadOnlyMemory<byte> memory)
    {
        if (memory.Length == 0)
        {
            return Null;
        }

        var interopBytes = NativeMemory.Alloc((nuint)memory.Length);

        memory.Span.CopyTo(new Span<byte>(interopBytes, memory.Length));

        return new InteropArray((byte*)interopBytes, memory.Length);
    }

    public static InteropArray Utf8FromString(string str)
    {
        if (str.Length == 0)
        {
            return Null;
        }

        var utf8BufferLength = Encoding.UTF8.GetMaxByteCount(str.Length);
        var utf8Buffer = NativeMemory.Alloc((nuint)utf8BufferLength);

        var utf8Length = Encoding.UTF8.GetBytes(str, new Span<byte>(utf8Buffer, utf8BufferLength));

        return new InteropArray((byte*)utf8Buffer, utf8Length);
    }

    public byte[] ToArray()
    {
        return !IsNullOrEmpty ? new ReadOnlySpan<byte>(_bytes, (int)_length).ToArray() : [];
    }

    public byte[]? ToArrayOrNull()
    {
        return !IsNullOrEmpty ? new ReadOnlySpan<byte>(_bytes, (int)_length).ToArray() : null;
    }

    public string Utf8ToString()
    {
        return !IsNullOrEmpty ? Encoding.UTF8.GetString(_bytes, (int)_length) : string.Empty;
    }

    public string? Utf8ToStringOrNull()
    {
        return !IsNullOrEmpty ? Encoding.UTF8.GetString(_bytes, (int)_length) : null;
    }

    public ReadOnlySpan<byte> AsReadOnlySpan()
    {
        return !IsNullOrEmpty ? new ReadOnlySpan<byte>(_bytes, (int)_length) : null;
    }
}

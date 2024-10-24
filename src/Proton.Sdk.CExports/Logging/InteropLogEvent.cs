using System.Runtime.InteropServices;

namespace Proton.Sdk.CExports.Logging;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropLogEvent(byte level, InteropArray message, InteropArray categoryName)
{
    public readonly byte Level = level;
    public readonly InteropArray Message = message;
    public readonly InteropArray CategoryName = categoryName;
}

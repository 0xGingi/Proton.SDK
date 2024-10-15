using System.Runtime.InteropServices;

namespace Proton.Sdk.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropSdkError(int code, InteropArray message)
{
    public readonly int Code = code;
    public readonly InteropArray Message = message;

    public static InteropSdkError FromManaged(SdkError error)
    {
        return new InteropSdkError(error.Code, InteropArray.Utf8FromString(error.Message));
    }
}

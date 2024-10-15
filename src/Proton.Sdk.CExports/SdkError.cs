namespace Proton.Sdk.CExports;

internal readonly struct SdkError(int code, string message)
{
    public readonly int Code = code;
    public readonly string Message = message;
}

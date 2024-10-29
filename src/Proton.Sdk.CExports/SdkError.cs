namespace Proton.Sdk.CExports;

internal readonly struct SdkError(int code, string message)
{
    public readonly int Code = code;
    public readonly string Message = message;

    internal static SdkError FromException(Exception ex)
    {
        if (ex is ProtonApiException protonApiException)
        {
            return new SdkError((int)protonApiException.Code, protonApiException.Message);
        }
        else
        {
            return new SdkError(-1, ex.Message);
        }
    }
}

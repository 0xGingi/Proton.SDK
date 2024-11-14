using Google.Protobuf;

namespace Proton.Sdk.CExports;

public struct ResultExtensions
{
    internal static Result<InteropArray, InteropArray> Success()
    {
        return new Result<InteropArray, InteropArray>(value: InteropArray.Null);
    }

    internal static Result<InteropArray, InteropArray> Success(IMessage data)
    {
        return new Result<InteropArray, InteropArray>(
            value: InteropArray.FromMemory(data.ToByteArray()));
    }

    internal static Result<InteropArray, InteropArray> Success(int value)
    {
        return new Result<InteropArray, InteropArray>(
            value: InteropArray.FromMemory(new IntResponse { Value = value }.ToByteArray()));
    }

    internal static Result<InteropArray, InteropArray> Success(string value)
    {
        return new Result<InteropArray, InteropArray>(
            value: InteropArray.FromMemory(new StringResponse { Value = value }.ToByteArray()));
    }

    internal static Result<InteropArray, InteropArray> Failure(Exception ex, int defaultCode)
    {
        if (ex is ProtonApiException protonEx)
        {
            return Failure((int)protonEx.Code, protonEx.Message);
        }
        else
        {
            return Failure(defaultCode, ex.Message);
        }
    }

    private static Result<InteropArray, InteropArray> Failure(int code, string message)
    {
        return new Result<InteropArray, InteropArray>(
            error: InteropArray.FromMemory(new Error { PrimaryCode = code, Message = message }.ToByteArray()));
    }

    internal static Result<InteropArray, InteropArray> Failure(Exception exception)
    {
        var error = exception.ToInteropError();

        return new Result<InteropArray, InteropArray>(error: InteropArray.FromMemory(error.ToByteArray()));
    }
}

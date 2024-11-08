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

    internal static Result<InteropArray, InteropArray> Failure(int code, string message)
    {
        return new Result<InteropArray, InteropArray>(
            error: InteropArray.FromMemory(new ErrorResponse { Code = code, Message = message }.ToByteArray()));
    }
}

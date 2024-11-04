namespace Proton.Sdk;

internal sealed class ProtonApiException<T> : ProtonApiException
    where T : ApiResponse
{
    public ProtonApiException()
    {
    }

    public ProtonApiException(string message)
        : base(message)
    {
    }

    public ProtonApiException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ProtonApiException(T response)
        : base(response)
    {
        Response = response;
    }

    public T? Response { get; }
}

namespace Proton.Sdk;

public class ProtonApiException : Exception
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
}

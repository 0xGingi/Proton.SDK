using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Polly.Timeout;

namespace Proton.Sdk.CExports;

internal static class ExceptionExtensions
{
    public static Error ToInteropError(this Exception exception)
    {
        var error = new Error
        {
            Message = exception.Message,
        };

        var type = exception.GetType().FullName;
        if (type is not null)
        {
            error.Type = type;
        }

        var context = exception.StackTrace;
        if (context is not null)
        {
            error.Context = context;
        }

        switch (exception)
        {
            case OperationCanceledException:
                error.Domain = ErrorDomain.SuccessfulCancellation;
                break;

            case ProtonApiException ex:
                error.Domain = ErrorDomain.Api;
                error.PrimaryCode = (long)ex.Code;
                error.SecondaryCode = ex.TransportCode;
                break;

            case SocketException ex:
                error.Domain = ErrorDomain.Network;
                error.PrimaryCode = ex.ErrorCode;
                error.SecondaryCode = (long)ex.SocketErrorCode;
                break;

            case HttpRequestException ex:
                error.Domain = ErrorDomain.Transport;
                error.PrimaryCode = (long)ex.HttpRequestError;
                error.SecondaryCode = ex.StatusCode is not null ? (long)ex.StatusCode : 0;
                break;

            case TimeoutRejectedException:
                error.Domain = ErrorDomain.Transport;
                error.PrimaryCode = (long)HttpRequestError.ConnectionError;
                break;

            case HttpIOException ex:
                error.Domain = ErrorDomain.Transport;
                error.PrimaryCode = (long)ex.HttpRequestError;
                break;

            case JsonException:
                error.Domain = ErrorDomain.Serialization;
                break;

            case CryptographicException:
                error.Domain = ErrorDomain.Cryptography;
                break;
        }

        error.InnerError = exception.InnerException?.ToInteropError();

        return error;
    }
}

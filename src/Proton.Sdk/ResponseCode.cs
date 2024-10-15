using System.Net;

namespace Proton.Sdk;

public enum ResponseCode
{
    Unknown = 0,

    Unauthorized = HttpStatusCode.Unauthorized,
    Forbidden = HttpStatusCode.Forbidden,
    RequestTimeout = HttpStatusCode.RequestTimeout,

    Success = 1000,
    MultipleResponses = 1001,
    ParentDoesNotExist = 2000,
    InvalidValue = 2001,
    InvalidEncryptedIdFormat = 2061,
    Timeout = 2503,
    AlreadyExists = 2500,
    DoesNotExist = 2501,
    InvalidApp = 5002,
    OutdatedApp = 5003,
    Offline = 7001,
    IncorrectLoginCredentials = 8002,

    /// <summary>
    /// Account is disabled
    /// </summary>
    AccountDeleted = 10002,

    /// <summary>
    /// Account is disabled due to abuse or fraud
    /// </summary>
    AccountDisabled = 10003,

    /// <summary>
    /// Free account
    /// </summary>
    NoActiveSubscription = 22110,

    CustomCode = 10000000,
    SocketError = CustomCode + 1,
    SessionRefreshFailed = CustomCode + 3,
    SrpError = CustomCode + 4,
}

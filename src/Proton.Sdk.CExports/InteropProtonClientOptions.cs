using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Proton.Sdk.CExports.Logging;

namespace Proton.Sdk.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropProtonClientOptions
{
    public readonly InteropArray AppVersion;
    public readonly InteropArray UserAgent;
    public readonly InteropArray BaseUrl;
    public readonly bool IgnoreSslCertificateErrors;
    public readonly nint LoggerProviderHandle;

    public ProtonClientOptions ToManaged()
    {
        var baseUrl = BaseUrl.Utf8ToStringOrNull();

        InteropLoggerProvider.TryGetFromHandle(LoggerProviderHandle, out var loggerProvider);

        return new ProtonClientOptions
        {
            AppVersion = AppVersion.Utf8ToStringOrNull(),
            UserAgent = UserAgent.Utf8ToStringOrNull(),
            BaseUrl = baseUrl is not null ? new Uri(baseUrl) : null,
            IgnoreSslCertificateErrors = IgnoreSslCertificateErrors,
            LoggerFactory = loggerProvider is not null ? new LoggerFactory([loggerProvider]) : null,
        };
    }
}

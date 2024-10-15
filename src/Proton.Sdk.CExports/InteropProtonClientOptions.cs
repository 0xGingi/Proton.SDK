using System.Runtime.InteropServices;

namespace Proton.Sdk.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropProtonClientOptions
{
    public readonly InteropArray AppVersion;
    public readonly InteropArray UserAgent;
    public readonly InteropArray BaseUrl;
    public readonly bool IgnoreSslCertificateErrors;

    public ProtonClientOptions ToManaged()
    {
        var baseUrl = BaseUrl.Utf8ToStringOrNull();

        return new ProtonClientOptions
        {
            AppVersion = AppVersion.Utf8ToStringOrNull(),
            UserAgent = UserAgent.Utf8ToStringOrNull(),
            BaseUrl = baseUrl is not null ? new Uri(baseUrl) : null,
            IgnoreSslCertificateErrors = IgnoreSslCertificateErrors,
        };
    }
}

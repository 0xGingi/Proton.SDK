using Microsoft.Extensions.Logging;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk;

public sealed class ProtonClientOptions
{
    public Uri? BaseUrl { get; set; }
    public string? AppVersion { get; set; }
    public string? UserAgent { get; set; }
    public bool? IgnoreSslCertificateErrors { get; set; }
    public ISecretsCache? SecretsCache { get; set; }
    public ILoggerFactory? LoggerFactory { get; set; }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk;

internal sealed class ProtonClientConfiguration(ProtonClientOptions options)
{
    public Uri BaseUrl { get; } = options.BaseUrl ?? ProtonApiDefaults.BaseUrl;
    public string AppVersion { get; } = options.AppVersion ?? string.Empty;
    public string UserAgent { get; } = options.UserAgent ?? string.Empty;
    public bool IgnoreSslCertificateErrors { get; } = options.IgnoreSslCertificateErrors ?? false;
    public ISecretsCache SecretsCache { get; } = options.SecretsCache ?? new InMemorySecretsCache();
    public ILoggerFactory LoggerFactory { get; } = options.LoggerFactory ?? NullLoggerFactory.Instance;
}

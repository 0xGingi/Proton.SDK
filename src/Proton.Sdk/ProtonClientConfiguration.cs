using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk;

internal sealed class ProtonClientConfiguration(ProtonClientOptions options)
{
    public string BaseUrl { get; } = options.BaseUrl ?? ProtonApiDefaults.BaseUrl.AbsolutePath;
    public string AppVersion { get; } = options.AppVersion ?? string.Empty;
    public string UserAgent { get; } = options.UserAgent ?? string.Empty;
    public bool DisableTlsPinning { get; } = options.HasDisableTlsPinning && options.DisableTlsPinning;
    public bool IgnoreSslCertificateErrors { get; } = options.HasIgnoreSslCertificateErrors && options.IgnoreSslCertificateErrors;
    public ISecretsCache SecretsCache { get; } = options.SecretsCache ?? new InMemorySecretsCache();
    public ILoggerFactory LoggerFactory { get; } = options.LoggerFactory ?? NullLoggerFactory.Instance;
    public Func<DelegatingHandler>? CustomHttpMessageHandlerFactory { get; } = options.CustomHttpMessageHandlerFactory;
}

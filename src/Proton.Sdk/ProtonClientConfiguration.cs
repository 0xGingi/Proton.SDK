using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk;

internal sealed class ProtonClientConfiguration(ProtonClientOptions options)
{
    public string AppVersion { get; } = options.AppVersion;
    public string BaseUrl { get; } = options.HasBaseUrl ? options.BaseUrl : ProtonApiDefaults.BaseUrl.ToString();
    public string? UserAgent { get; } = options.HasUserAgent ? options.UserAgent : null;
    public string? BindingsLanguage { get; } = options.BindingsLanguage;
    public bool DisableTlsPinning { get; } = options.HasDisableTlsPinning && options.DisableTlsPinning;
    public bool IgnoreSslCertificateErrors { get; } = options.HasIgnoreSslCertificateErrors && options.IgnoreSslCertificateErrors;
    public ISecretsCache SecretsCache { get; } = options.SecretsCache ?? new InMemorySecretsCache();
    public ILoggerFactory LoggerFactory { get; } = options.LoggerFactory ?? NullLoggerFactory.Instance;
    public Func<DelegatingHandler>? CustomHttpMessageHandlerFactory { get; } = options.CustomHttpMessageHandlerFactory;
}

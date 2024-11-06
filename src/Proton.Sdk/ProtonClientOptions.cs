using Microsoft.Extensions.Logging;
using Proton.Sdk.Cryptography;

namespace Proton.Sdk;

public sealed partial class ProtonClientOptions
{
    public ISecretsCache? SecretsCache { get; set; }
    public ILoggerFactory? LoggerFactory { get; set; }
    public Func<DelegatingHandler>? CustomHttpMessageHandlerFactory { get; set; }
}

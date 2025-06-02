using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Proton.Sdk.Authentication;
using Proton.Sdk.Http;

namespace Proton.Sdk;

internal static class ProtonClientConfigurationExtensions
{
    private static readonly CookieContainer CookieContainer = new();

    public static HttpClient GetHttpClient(
        this ProtonClientConfiguration config,
        ProtonApiSession? session = null,
        string? baseRoutePath = null,
        TimeSpan? attemptTimeout = null)
    {
        var baseAddress = config.BaseUrl + (baseRoutePath ?? string.Empty);

        var services = new ServiceCollection();

        services.AddSingleton(config.LoggerFactory);

        var socketLogger = config.LoggerFactory.CreateLogger("Socket");

        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.UseSocketsHttpHandler((handler, _) =>
            {
                handler.AddAutomaticDecompression();
                handler.ConfigureCookies(CookieContainer);

                if (config.IgnoreSslCertificateErrors)
                {
                    handler.SslOptions.RemoteCertificateValidationCallback += (_, _, _, _) => true;
                }
                else if (!config.DisableTlsPinning)
                {
                    handler.AddTlsPinning();
                }

                handler.ConnectCallback += (context, cancellationToken)
                    => BrokenPipeHandlingStream.CreateAsync(context, socketLogger, cancellationToken);
            });

            builder.AddHttpMessageHandler(() => new CryptographyTimeProvisionHandler());

            if (config.CustomHttpMessageHandlerFactory is not null)
            {
                builder.AddHttpMessageHandler(() => config.CustomHttpMessageHandlerFactory.Invoke());
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                builder.AddResilienceHandler(
                    "macos-broken-pipe",
                    resilienceBuilder =>
                    {
                        resilienceBuilder.AddRetry(
                            new HttpRetryStrategyOptions
                            {
                                MaxRetryAttempts = BrokenPipeHandlingStream.SuggestedNumberOfRetries,
                                Delay = TimeSpan.FromMilliseconds(100),
                                BackoffType = DelayBackoffType.Constant,
                                ShouldHandle = arguments => ValueTask.FromResult(IsPosixBrokenPipe(arguments.Outcome.Exception)),
                            });
                    });
            }

            builder.AddStandardResilienceHandler(options =>
            {
                if (attemptTimeout is not null)
                {
                    options.AttemptTimeout.Timeout = attemptTimeout.Value;
                    options.CircuitBreaker.SamplingDuration = options.AttemptTimeout.Timeout * 3;
                }

                options.Retry.ShouldRetryAfterHeader = true;
                options.Retry.Delay = TimeSpan.FromSeconds(2.5);
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;
                options.Retry.MaxRetryAttempts = 4;

                var totalTimeout = (options.AttemptTimeout.Timeout + options.Retry.Delay) * options.Retry.MaxRetryAttempts * 1.5;
                options.TotalRequestTimeout = new HttpTimeoutStrategyOptions { Timeout = totalTimeout };

                options.CircuitBreaker.FailureRatio = 0.5;
            });

            if (session is not null)
            {
                builder.AddHttpMessageHandler(() => new AuthorizationHandler(session));
            }

            builder.ConfigureHttpClient(httpClient =>
            {
                var executingAssembly = Assembly.GetExecutingAssembly();
                var versionAttribute = executingAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var sdkVersion = versionAttribute?.InformationalVersion
                    ?? executingAssembly.GetName().Version?.ToString(fieldCount: 3)
                    ?? "0.0.0";

                var bindingsSuffix = config.BindingsLanguage is not null ? "-" + config.BindingsLanguage.ToLowerInvariant() : string.Empty;
                var sdkTechnicalStack = "dotnet" + bindingsSuffix;

                httpClient.BaseAddress = new Uri(baseAddress);
                httpClient.DefaultRequestHeaders.Add("x-pm-appversion", config.AppVersion);
                httpClient.DefaultRequestHeaders.Add("x-pm-drive-sdk-version", $"{sdkTechnicalStack}@{sdkVersion}");

                if (config.UserAgent is not null)
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", config.UserAgent);
                }
            });
        });

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<HttpClient>();
    }

    private static bool IsPosixBrokenPipe(Exception? exception)
        => exception?.GetBaseException() is SocketException { ErrorCode: BrokenPipeHandlingStream.PosixEpipeErrorCode }
            or SocketException { SocketErrorCode: SocketError.Shutdown };
}

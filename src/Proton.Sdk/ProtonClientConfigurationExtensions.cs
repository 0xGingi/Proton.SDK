using Microsoft.Extensions.DependencyInjection;
using Polly;
using Proton.Sdk.Authentication;

namespace Proton.Sdk;

internal static class ProtonClientConfigurationExtensions
{
    public static HttpClient GetHttpClient(this ProtonClientConfiguration config, ProtonApiSession? session = null, string? baseRoutePath = default)
    {
        var baseAddress = baseRoutePath is not null ? new Uri(config.BaseUrl, baseRoutePath) : config.BaseUrl;

        var services = new ServiceCollection();

        services.ConfigureHttpClientDefaults(
            builder =>
            {
                if (config.IgnoreSslCertificateErrors)
                {
                    builder.UseSocketsHttpHandler((handler, _) => handler.SslOptions.RemoteCertificateValidationCallback += (_, _, _, _) => true);
                }

                builder.AddStandardResilienceHandler(
                    options =>
                    {
                        options.Retry.ShouldRetryAfterHeader = true;
                        options.Retry.Delay = TimeSpan.FromSeconds(10);
                        options.Retry.BackoffType = DelayBackoffType.Exponential;
                        options.Retry.UseJitter = true;
                        options.Retry.MaxRetryAttempts = 4;
                        options.CircuitBreaker.FailureRatio = 0.8;
                    });

                if (session is not null)
                {
                    builder.AddHttpMessageHandler(() => new AuthorizationHandler(session));
                }

                builder.ConfigureHttpClient(httpClient =>
                {
                    httpClient.BaseAddress = baseAddress;
                    httpClient.DefaultRequestHeaders.Add("x-pm-appversion", config.AppVersion);
                    httpClient.DefaultRequestHeaders.Add("User-Agent", config.UserAgent);
                });
            });

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<HttpClient>();
    }
}

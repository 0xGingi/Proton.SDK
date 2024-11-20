﻿using System.Net;
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
        string? baseRoutePath = default,
        TimeSpan? attemptTimeout = default)
    {
        var baseAddress = config.BaseUrl + (baseRoutePath ?? string.Empty);

        var services = new ServiceCollection();

        services.AddSingleton(config.LoggerFactory);

        services.ConfigureHttpClientDefaults(
            builder =>
            {
                builder.UseSocketsHttpHandler(
                    (handler, _) =>
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
                    });

                if (config.CustomHttpMessageHandlerFactory is not null)
                {
                    builder.AddHttpMessageHandler(() => config.CustomHttpMessageHandlerFactory.Invoke());
                }

                builder.AddStandardResilienceHandler(
                    options =>
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

                builder.ConfigureHttpClient(
                    httpClient =>
                    {
                        httpClient.BaseAddress = new Uri(baseAddress);
                        httpClient.DefaultRequestHeaders.Add("x-pm-appversion", config.AppVersion);
                        httpClient.DefaultRequestHeaders.Add("User-Agent", config.UserAgent);
                    });
            });

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider.GetRequiredService<HttpClient>();
    }
}

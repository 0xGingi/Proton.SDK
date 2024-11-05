using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Proton.Sdk.Instrumentation.Extensions;
using Proton.Sdk.Instrumentation.Metrics;

namespace Proton.Sdk.Instrumentation.Provider;

public sealed class ObservabilityService : Meter, IObservabilityService
{
    private readonly ObservabilityApiClient _observabilityApiClient;
    private readonly TimeSpan _period;
    private readonly ConcurrentBag<InstrumentRegistration> _instrumentRegistrations = new();

    private CancellationTokenSource _cancellationTokenSource = new();
    private PeriodicTimer _timer;
    private Task? _timerTask;

    public ObservabilityService(ProtonApiSession session)
    {
        _observabilityApiClient = new ObservabilityApiClient(session.GetHttpClient(ProtonInstrumentationDefaults.ObservabilityBaseRoute));
        _period = JitterGenerator.ApplyJitter(ProtonInstrumentationDefaults.DefaultReportingInterval, 0.2);
        _timer = new PeriodicTimer(_period);
    }

    public static ObservabilityService StartNew(ProtonApiSession session)
    {
        var service = new ObservabilityService(session);
        service.Start();
        return service;
    }

    public void Start()
    {
        if (_timerTask is not null)
        {
            return;
        }

        _timerTask = PeriodicallySendMetricsAsync(_cancellationTokenSource.Token);
    }

    public void Stop()
    {
        if (_timerTask is null)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        _timer.Dispose();
        _timer = new PeriodicTimer(_period);
    }

    public async ValueTask FlushAsync(CancellationToken cancellationToken)
    {
        await SendMetricsAsync(cancellationToken).ConfigureAwait(false);
    }

    public override ICounter CreateCounter(string name, int version, JsonNode labels)
    {
        var counter = new Counter();
        _instrumentRegistrations.Add(new InstrumentRegistration(name, version, labels, counter));
        return counter;
    }

    private async Task PeriodicallySendMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await SendMetricsAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Do nothing
        }
    }

    private async Task SendMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var uploadMetrics = GetObservabilityMetrics();

            if (uploadMetrics.Count == 0)
            {
                return;
            }

            var metrics = new ObservabilityMetricsParameters(uploadMetrics);

            await _observabilityApiClient.SendMetricsAsync(metrics, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Ignore failure
        }
        finally
        {
            ResetInstruments();
        }
    }

    private ReadOnlyCollection<ObservabilityMetricDto> GetObservabilityMetrics()
    {
        var result = _instrumentRegistrations
            .Where(x => x.Instrument is Counter { Value: > 0 })
            .Select(
                c => new ObservabilityMetricDto(
                    c.Name,
                    c.Version,
                    DateTime.UtcNow.ToUnixTimeSeconds(),
                    new ObservabilityMetricPropertiesDto(((Counter)c.Instrument).Value, c.Labels)))
            .ToList();

        return result.Count > 0 ? new ReadOnlyCollection<ObservabilityMetricDto>(result) : ReadOnlyCollection<ObservabilityMetricDto>.Empty;
    }

    private void ResetInstruments()
    {
        foreach (var meter in _instrumentRegistrations)
        {
            meter.Instrument.Reset();
        }
    }
}

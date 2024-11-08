using System.Collections.ObjectModel;
using Proton.Sdk.Instrumentation.Extensions;

namespace Proton.Sdk.Instrumentation.Observability;

public sealed class ObservabilityService : IInstrumentFactory, IObservabilityService
{
    private readonly ObservabilityApiClient _observabilityApiClient;
    private readonly TimeSpan _period;
    private readonly List<CounterDefinition> _counterDefinitions = new(capacity: 16);

    private CancellationTokenSource _cancellationTokenSource = new();
    private PeriodicTimer _timer;
    private Task? _timerTask;

    public ObservabilityService(ProtonApiSession session)
    {
        _observabilityApiClient = new ObservabilityApiClient(session.GetHttpClient(ProtonInstrumentationDefaults.ObservabilityBaseRoute));
        _period = JitterGenerator.ApplyJitter(ProtonInstrumentationDefaults.DefaultReportingInterval, 0.2);
        _timer = new PeriodicTimer(_period);
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

    ICounter IInstrumentFactory.CreateCounter(string name, int version, object labels)
    {
        var counter = new Counter();
        _counterDefinitions.Add(new CounterDefinition(name, version, labels, counter));
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
        var uploadMetrics = GetMetrics();

        if (uploadMetrics.Count == 0)
        {
            return;
        }

        var metrics = new ObservabilityMetricsParameters(uploadMetrics);

        await _observabilityApiClient.SendMetricsAsync(metrics, cancellationToken).ConfigureAwait(false);

        ResetCounters();
    }

    private ReadOnlyCollection<ObservabilityMetric> GetMetrics()
    {
        return _counterDefinitions
            .ConvertAll(
                c => new ObservabilityMetric(
                    c.Name,
                    c.Version,
                    DateTime.UtcNow.ToUnixTimeSeconds(),
                    new ObservabilityMetricProperties(c.Counter.Value, c.Labels)))
            .AsReadOnly();
    }

    private void ResetCounters()
    {
        foreach (var counterDefinition in _counterDefinitions)
        {
            counterDefinition.Counter.Reset();
        }
    }
}

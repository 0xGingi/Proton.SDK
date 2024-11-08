namespace Proton.Sdk.Instrumentation.Observability;

public interface IObservabilityService
{
    void Start();
    void Stop();
    ValueTask FlushAsync(CancellationToken cancellationToken);
}

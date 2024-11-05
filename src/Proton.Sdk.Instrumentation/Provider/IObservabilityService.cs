namespace Proton.Sdk.Instrumentation.Provider;

public interface IObservabilityService
{
    void Start();
    void Stop();
    ValueTask FlushAsync(CancellationToken cancellationToken);
}

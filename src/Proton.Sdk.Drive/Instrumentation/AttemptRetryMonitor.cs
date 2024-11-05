using Proton.Sdk.Instrumentation.Metrics;

namespace Proton.Sdk.Drive.Instrumentation;

internal sealed class AttemptRetryMonitor<TId>(
    ICounter firstAttemptSuccessCounter,
    ICounter firstAttemptFailureCounter,
    ICounter retriedSuccessCounter,
    ICounter retriedFailureCounter)
    where TId : notnull
{
    private readonly object _lock = new();
    private readonly Dictionary<TId, AttemptType> _statusByItemId = [];
    private readonly ICounter _firstAttemptSuccessCounter = firstAttemptSuccessCounter;
    private readonly ICounter _firstAttemptFailureCounter = firstAttemptFailureCounter;
    private readonly ICounter _retriedSuccessCounter = retriedSuccessCounter;
    private readonly ICounter _retriedFailureCounter = retriedFailureCounter;

    public void IncrementSuccess(TId id)
    {
        lock (_lock)
        {
            if (!_statusByItemId.Remove(id))
            {
                _firstAttemptSuccessCounter.Increment();
                return;
            }

            _retriedSuccessCounter.Increment();
        }
    }

    public void IncrementFailure(TId id)
    {
        lock (_lock)
        {
            if (!_statusByItemId.TryGetValue(id, out var itemStatus))
            {
                _statusByItemId.Add(id, AttemptType.FirstAttempt);
                _firstAttemptFailureCounter.Increment();
                return;
            }

            if (itemStatus == AttemptType.FirstAttempt)
            {
                _statusByItemId[id] = AttemptType.Retry;
            }

            _retriedFailureCounter.Increment();
        }
    }
}

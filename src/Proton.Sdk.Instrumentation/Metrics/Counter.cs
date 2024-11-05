namespace Proton.Sdk.Instrumentation.Metrics;

internal sealed class Counter : Instrument, ICounter
{
    private int _value;

    public int Value => _value;

    public void Increment()
    {
        Interlocked.Increment(ref _value);
    }

    public override void Reset()
    {
        Interlocked.Exchange(ref _value, 0);
    }
}

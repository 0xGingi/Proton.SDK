namespace Proton.Sdk.Instrumentation;

internal sealed class Counter : ICounter
{
    private int _value;

    public int Value => _value;

    public void Increment()
    {
        Interlocked.Increment(ref _value);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _value, 0);
    }
}

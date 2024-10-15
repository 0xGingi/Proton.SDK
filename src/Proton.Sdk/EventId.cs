namespace Proton.Sdk;

public readonly record struct EventId(string Value)
{
    internal string Value { get; } = Value;

    public override string ToString()
    {
        return Value;
    }
}

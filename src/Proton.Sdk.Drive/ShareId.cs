namespace Proton.Sdk.Drive;

public readonly record struct ShareId(string Value)
{
    internal string Value { get; } = Value;

    public override string ToString()
    {
        return Value;
    }
}

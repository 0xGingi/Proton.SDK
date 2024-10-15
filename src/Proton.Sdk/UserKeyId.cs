namespace Proton.Sdk;

public readonly struct UserKeyId(string value)
{
    internal string Value { get; } = value;

    public override string ToString()
    {
        return Value;
    }
}

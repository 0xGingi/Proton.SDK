namespace Proton.Sdk;

public readonly struct AddressKeyId(string value)
{
    internal string Value { get; } = value;

    public override string ToString()
    {
        return Value;
    }
}

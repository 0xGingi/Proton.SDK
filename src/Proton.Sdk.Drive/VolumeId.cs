namespace Proton.Sdk.Drive;

public readonly record struct VolumeId(string Value)
{
    internal string Value { get; } = Value;

    public override string ToString()
    {
        return Value;
    }
}

namespace Proton.Sdk;

public interface IFormattableValue : IFormattable
{
    public string Value { get; }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        return Value;
    }
}

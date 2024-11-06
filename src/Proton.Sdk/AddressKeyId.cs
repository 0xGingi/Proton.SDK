namespace Proton.Sdk;

public sealed partial class AddressKeyId : IFormattableValue
{
    public AddressKeyId(string str)
        : this(new AddressKeyId { Value = str }) { }
}

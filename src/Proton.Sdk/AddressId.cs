namespace Proton.Sdk;

public sealed partial class AddressId : IFormattableValue
{
    public AddressId(string str)
        : this(new AddressId { Value = str }) { }
}

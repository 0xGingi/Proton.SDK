namespace Proton.Sdk;

public sealed partial class UserKeyId : IFormattableValue
{
    public UserKeyId(string str)
        : this(new UserKeyId { Value = str }) { }
}

namespace Proton.Sdk;

public sealed partial class UserId : IFormattableValue
{
    public UserId(string str)
        : this(new UserId { Value = str }) { }
}

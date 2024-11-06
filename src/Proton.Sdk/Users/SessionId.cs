namespace Proton.Sdk;

public sealed partial class SessionId : IFormattableValue
{
    public SessionId(string str)
        : this(new SessionId { Value = str }) { }
}

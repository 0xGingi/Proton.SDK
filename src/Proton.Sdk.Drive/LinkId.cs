namespace Proton.Sdk.Drive;

public sealed partial class LinkId : IFormattableValue
{
    public LinkId(string str)
        : this(new LinkId { Value = str }) { }
}

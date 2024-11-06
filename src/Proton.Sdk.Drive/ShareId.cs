namespace Proton.Sdk.Drive;

public sealed partial class ShareId : IFormattableValue
{
    public ShareId(string str)
        : this(new ShareId { Value = str }) { }
}

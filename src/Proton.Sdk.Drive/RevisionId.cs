namespace Proton.Sdk.Drive;

public sealed partial class RevisionId : IFormattableValue
{
    public RevisionId(string str)
        : this(new RevisionId { Value = str }) { }
}

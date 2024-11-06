namespace Proton.Sdk.Drive;

public sealed partial class VolumeId : IFormattableValue
{
    public VolumeId(string str)
        : this(new VolumeId { Value = str }) { }
}

using System.Runtime.InteropServices;
using Proton.Sdk.CExports;

namespace Proton.Sdk.Drive.CExports;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct InteropShareForCommand
{
    public readonly InteropArray Id;
    public readonly InteropArray MembershipAddressId;
    public readonly InteropArray MembershipEmailAddress;

    public IShareForCommand ToManaged()
    {
        return new Managed
        {
            Id = new ShareId(Id.Utf8ToString()),
            MembershipAddressId = new AddressId(MembershipAddressId.Utf8ToString()),
            MembershipEmailAddress = MembershipEmailAddress.Utf8ToString(),
        };
    }

    private sealed class Managed : IShareForCommand
    {
        public required ShareId Id { get; init; }

        public required AddressId MembershipAddressId { get; init; }

        public required string MembershipEmailAddress { get; init; }
    }
}

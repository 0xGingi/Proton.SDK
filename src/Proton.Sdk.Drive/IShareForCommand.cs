namespace Proton.Sdk.Drive;

public interface IShareForCommand
{
    ShareId ShareId { get; }
    AddressId MembershipAddressId { get; }
    string MembershipEmailAddress { get; }
}

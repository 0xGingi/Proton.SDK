namespace Proton.Sdk.Drive;

public interface IRevisionShareBasedIdentity
{
    ShareId ShareId { get; }
    LinkId FileId { get; }
    RevisionId Id { get; }
}

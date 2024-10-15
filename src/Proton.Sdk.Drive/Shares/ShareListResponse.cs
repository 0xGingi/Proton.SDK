namespace Proton.Sdk.Drive.Shares;

internal sealed class ShareListResponse : ApiResponse
{
    public required IReadOnlyList<ShareListItem> Shares { get; init; }
}

using Proton.Sdk.Drive.Links;

namespace Proton.Sdk.Drive.Folders;

internal sealed class FolderChildListResponse : ApiResponse
{
    public required IReadOnlyList<Link> Links { get; init; }
}

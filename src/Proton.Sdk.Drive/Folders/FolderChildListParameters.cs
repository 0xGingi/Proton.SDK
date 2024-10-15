namespace Proton.Sdk.Drive.Folders;

internal sealed class FolderChildListParameters
{
    public int PageIndex { get; set; }
    public int? PageSize { get; set; }
    public bool ShowAll { get; set; }
}

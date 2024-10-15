namespace Proton.Sdk.Drive.Volumes;

public enum VolumeEventType
{
    /// <summary>
    /// A file or folder was garbage collected or moved out of share view.
    /// </summary>
    Delete = 0,

    /// <summary>
    /// A file or folder was created or moved into a share view. For files, it is
    /// generated when the first revision is committed.
    /// </summary>
    Create = 1,

    /// <summary>
    /// File contents were updated.
    /// </summary>
    Update = 2,

    /// <summary>
    /// File or folder metadata was updated. Includes updates to name, parent link,
    /// shares, share URLs, and state (active, trashed, permanently deleted).
    /// </summary>
    UpdateMetadata = 3,
}

namespace Proton.Sdk.Drive.Instrumentation;

internal enum AttemptRetryShareType
{
    /// <summary>
    /// The share pointing to root node of the "My files" folder.
    /// </summary>
    Main,

    /// <summary>
    /// The share pointing to root node of the "My computers" folder (including any selected synced folders).
    /// </summary>
    Device,

    /// <summary>
    /// Normal share pointing to any node in the three structure. It is used for sharing between users.
    /// </summary>
    Standard,
}

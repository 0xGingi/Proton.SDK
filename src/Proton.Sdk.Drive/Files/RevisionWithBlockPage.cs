namespace Proton.Sdk.Drive.Files;

internal sealed class RevisionWithBlockPage : RevisionDto
{
    public required IReadOnlyList<Block> Blocks { get; init; }
}

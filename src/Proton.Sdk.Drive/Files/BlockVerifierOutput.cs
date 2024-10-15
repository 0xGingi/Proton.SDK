namespace Proton.Sdk.Drive.Files;

public readonly struct BlockVerifierOutput
{
    public required ReadOnlyMemory<byte> Token { get; init; }
}

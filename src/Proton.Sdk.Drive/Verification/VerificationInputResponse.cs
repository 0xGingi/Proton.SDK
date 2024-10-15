namespace Proton.Sdk.Drive.Verification;

internal sealed record VerificationInputResponse
{
    public required ReadOnlyMemory<byte> VerificationCode { get; init; }

    public required ReadOnlyMemory<byte> ContentKeyPacket { get; init; }
}

namespace Proton.Sdk.Authentication;

internal sealed class ScopesResponse : ApiResponse
{
    public required IReadOnlyList<string> Scopes { get; init; }
}

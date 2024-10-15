namespace Proton.Sdk.Drive;

internal sealed class AggregateResponse<T> : ApiResponse
{
    public required IReadOnlyList<T> Responses { get; init; }
}

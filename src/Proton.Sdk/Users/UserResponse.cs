namespace Proton.Sdk.Users;

internal sealed class UserResponse : ApiResponse
{
    public required User User { get; init; }
}

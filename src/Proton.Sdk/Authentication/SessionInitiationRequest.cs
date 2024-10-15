namespace Proton.Sdk.Authentication;

internal readonly struct SessionInitiationRequest(string username)
{
    public string Username => username;
}

namespace Proton.Sdk.Keys;

[Flags]
internal enum PublicKeyFlags
{
    IsNotCompromised = 1,
    IsNotObsolete = 2,
}

namespace Proton.Sdk.Events;

[Flags]
internal enum EventsRefreshMask : byte
{
    None = 0,
    Mail = 1,
    Contacts = 2,
#pragma warning disable RCS1157 // Composite enum value contains undefined flag
    All = 255,
#pragma warning restore RCS1157 // Composite enum value contains undefined flag
}

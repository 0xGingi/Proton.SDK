namespace Proton.Sdk.Drive.Devices;

internal sealed class DeviceListItem
{
    public required Device Device { get; init; }

    public required DeviceShare Share { get; init; }
}

namespace Proton.Sdk.Drive.Devices;

internal sealed class DeviceListResponse : ApiResponse
{
    public required IReadOnlyCollection<DeviceListItem> Devices { get; init; }
}

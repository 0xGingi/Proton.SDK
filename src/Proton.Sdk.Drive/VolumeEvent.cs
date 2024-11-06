using Proton.Sdk.Drive.Volumes;

namespace Proton.Sdk.Drive;

public readonly struct VolumeEvent(VolumeId volumeId, VolumeEventId eventId, VolumeEventType eventType, INode? node)
{
    public VolumeId VolumeId { get; } = volumeId;
    public VolumeEventId EventId { get; } = eventId;
    public VolumeEventType EventType { get; } = eventType;
    public INode? Node { get; } = node;
}

using Proton.Sdk.Drive.Volumes;

namespace Proton.Sdk.Drive;

public sealed class VolumeEventChannel(ProtonDriveClient client, VolumeId volumeId) : EventChannelBase<VolumeEventId>
{
    public event Action<Node>? NodeCreated;
    public event Action<Node>? NodeMetadataChanged;
    public event Action<FileNode>? FileContentsChanged;
    public event Action<VolumeId, LinkId>? NodeDeleted;

    public VolumeId VolumeId { get; } = volumeId;

    private ProtonDriveClient Client { get; } = client;

    private protected override IEventPoller CreateEventPoller()
    {
        return new EventPoller(this);
    }

    private async ValueTask DispatchEventsAsync(IReadOnlyList<EventDto> events, CancellationToken cancellationToken)
    {
        foreach (var @event in events)
        {
            switch (@event.Type)
            {
                case VolumeEventType.Create when @event is LinkEventDto linkEvent:
                    await DispatchNodeEventAsync(linkEvent, NodeCreated, cancellationToken).ConfigureAwait(false);
                    break;
                case VolumeEventType.UpdateMetadata when @event is LinkEventDto linkEvent:
                    await DispatchNodeEventAsync(linkEvent, NodeMetadataChanged, cancellationToken).ConfigureAwait(false);
                    break;
                case VolumeEventType.Update when @event is LinkEventDto linkEvent:
                    await DispatchFileContentsChangedEventAsync(linkEvent, cancellationToken).ConfigureAwait(false);
                    break;
                case VolumeEventType.Delete when @event is DeletedLinkEventDto deletedLinkEvent:
                    DispatchNodeDeletedEvent(deletedLinkEvent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private async ValueTask DispatchNodeEventAsync(LinkEventDto dto, Action<Node>? handlers, CancellationToken cancellationToken)
    {
        if (handlers == null)
        {
            return;
        }

        var node = await Node.GetAsync(Client, new ShareId(dto.ContextShareId), dto.Link, cancellationToken).ConfigureAwait(false);

        handlers.Invoke(node);
    }

    private async ValueTask DispatchFileContentsChangedEventAsync(LinkEventDto dto, CancellationToken cancellationToken)
    {
        var handlers = FileContentsChanged;
        if (handlers == null)
        {
            return;
        }

        var node = await Node.GetAsync(Client, new ShareId(dto.ContextShareId), dto.Link, cancellationToken).ConfigureAwait(false);
        if (node is not FileNode file)
        {
            return;
        }

        handlers.Invoke(file);
    }

    private void DispatchNodeDeletedEvent(DeletedLinkEventDto dto)
    {
        NodeDeleted?.Invoke(VolumeId, new LinkId(dto.Link.Id));
    }

    private sealed class EventPoller(VolumeEventChannel owner) : EventPollerBase<IReadOnlyList<EventDto>>
    {
        private readonly VolumeEventChannel _owner = owner;

        protected override EventChannelBase<VolumeEventId> Owner => _owner;

        protected override async ValueTask<VolumeEventId> GetLatestEventIdAsync(CancellationToken cancellationToken)
        {
            var response = await _owner.Client.VolumesApi.GetLatestEventAsync(_owner.VolumeId, cancellationToken).ConfigureAwait(false);

            return new VolumeEventId(response.EventId);
        }

        protected override async ValueTask<(IReadOnlyList<EventDto> Events, bool MoreEntriesExist, VolumeEventId LastEventId)> GetEventsAsync(
            VolumeEventId baselineEventId,
            CancellationToken cancellationToken)
        {
            var response = await _owner.Client.VolumesApi.GetEventsAsync(_owner.VolumeId, baselineEventId, cancellationToken).ConfigureAwait(false);

            return (response.Events, response.MoreEntriesExist, new VolumeEventId(response.LastEventId));
        }

        protected override ValueTask DispatchEventsAsync(IReadOnlyList<EventDto> events, CancellationToken cancellationToken)
        {
            return _owner.DispatchEventsAsync(events, cancellationToken);
        }
    }
}

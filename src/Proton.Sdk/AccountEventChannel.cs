using Proton.Sdk.Events;
namespace Proton.Sdk;

public sealed class AccountEventChannel(ProtonAccountClient client) : EventChannelBase<EventId>
{
    private readonly HashSet<IEventDispatcher> _dispatchers = [];

    public AccountEventChannel(ProtonApiSession session)
        : this(new ProtonAccountClient(session))
    {
    }

    internal ProtonAccountClient Client { get; } = client;

    internal void AddDispatcher(IEventDispatcher dispatcher)
    {
        _dispatchers.Add(dispatcher);
    }

    internal void RemoveDispatcher(IEventDispatcher dispatcher)
    {
        _dispatchers.Remove(dispatcher);
    }

    private protected override IEventPoller CreateEventPoller()
    {
        return new EventPoller(this);
    }

    private async ValueTask DispatchEventsAsync(EventListResponse events, CancellationToken cancellationToken)
    {
        foreach (var dispatcher in _dispatchers)
        {
            await dispatcher.DispatchEventsAsync(events, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class EventPoller(AccountEventChannel owner) : EventPollerBase<EventListResponse>
    {
        private readonly AccountEventChannel _owner = owner;

        protected override EventChannelBase<EventId> Owner => _owner;

        protected override async ValueTask<EventId> GetLatestEventIdAsync(CancellationToken cancellationToken)
        {
            var response = await _owner.Client.EventsApi.GetLatestEventAsync(cancellationToken).ConfigureAwait(false);

            return new EventId(response.EventId);
        }

        protected override async ValueTask<(EventListResponse Events, bool MoreEntriesExist, EventId LastEventId)> GetEventsAsync(
            EventId baselineEventId,
            CancellationToken cancellationToken)
        {
            var response = await _owner.Client.EventsApi.GetEventsAsync(baselineEventId, cancellationToken).ConfigureAwait(false);

            return (response, response.MoreEntriesExist, new EventId(response.LastEventId));
        }

        protected override ValueTask DispatchEventsAsync(EventListResponse events, CancellationToken cancellationToken)
        {
            return _owner.DispatchEventsAsync(events, cancellationToken);
        }
    }
}

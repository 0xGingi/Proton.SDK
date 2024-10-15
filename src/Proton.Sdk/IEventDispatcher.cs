using Proton.Sdk.Events;

namespace Proton.Sdk;

internal interface IEventDispatcher
{
    ValueTask DispatchEventsAsync(EventListResponse events, CancellationToken cancellationToken);
}

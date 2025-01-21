using Proton.Sdk.Addresses;
using Proton.Sdk.Events;

namespace Proton.Sdk;

public sealed class AddressEventDispatcher(AccountEventChannel eventChannel) : IEventDispatcher
{
    private readonly AccountEventChannel _eventChannel = eventChannel;

    private Action<Address>? _addressAddedHandlers;
    private Action<Address>? _addressChangedHandlers;
    private Action<AddressId>? _addressRemovedHandlers;

    public event Action<Address>? AddressAdded
    {
        add
        {
            StartDispatchingIfNoHandler();

            _addressAddedHandlers += value;
        }
        remove
        {
            _addressAddedHandlers -= value;

            StopDispatchingIfNoHandler();
        }
    }

    public event Action<Address>? AddressChanged
    {
        add
        {
            StartDispatchingIfNoHandler();

            _addressChangedHandlers += value;
        }
        remove
        {
            _addressChangedHandlers -= value;

            StopDispatchingIfNoHandler();
        }
    }

    public event Action<AddressId>? AddressRemoved
    {
        add
        {
            StartDispatchingIfNoHandler();

            _addressRemovedHandlers += value;
        }
        remove
        {
            _addressRemovedHandlers -= value;

            StopDispatchingIfNoHandler();
        }
    }

    async ValueTask IEventDispatcher.DispatchEventsAsync(EventListResponse events, CancellationToken cancellationToken)
    {
        if (events.AddressEvents is null)
        {
            return;
        }

        foreach (var addressEvent in events.AddressEvents)
        {
            switch (addressEvent.Action)
            {
                case EventAction.Create when addressEvent.Address is not null:
                    await DispatchAddressCreatedOrChangedAsync(addressEvent.Address, _addressAddedHandlers, cancellationToken).ConfigureAwait(false);
                    break;

                case EventAction.Update when addressEvent.Address is not null:
                case EventAction.UpdateFlags when addressEvent.Address is not null:
                    await DispatchAddressCreatedOrChangedAsync(addressEvent.Address, _addressChangedHandlers, cancellationToken).ConfigureAwait(false);
                    break;

                case EventAction.Delete:
                    _addressRemovedHandlers?.Invoke(new AddressId(addressEvent.AddressId));
                    break;
            }
        }
    }

    private void StartDispatchingIfNoHandler()
    {
        if (_addressAddedHandlers == null
            && _addressChangedHandlers == null
            && _addressRemovedHandlers == null)
        {
            _eventChannel.AddDispatcher(this);
        }
    }

    private void StopDispatchingIfNoHandler()
    {
        if (_addressAddedHandlers == null
            && _addressChangedHandlers == null
            && _addressRemovedHandlers == null)
        {
            _eventChannel.RemoveDispatcher(this);
        }
    }

    private async Task DispatchAddressCreatedOrChangedAsync(AddressDto addressDto, Action<Address>? handlers, CancellationToken cancellationToken)
    {
        if (handlers is null)
        {
            return;
        }

        var userKeys = await _eventChannel.Client.GetUserKeysAsync(cancellationToken).ConfigureAwait(false);
        var address = Address.FromDto(addressDto, userKeys, _eventChannel.Client.SecretsCache);
        handlers.Invoke(address);
    }
}

using System.Text.Json.Serialization;
using Proton.Sdk.Addresses;

namespace Proton.Sdk.Events;

internal sealed class AddressEvent
{
    public required EventAction Action { get; init; }

    [JsonPropertyName("ID")]
    public required string AddressId { get; init; }

    public AddressDto? Address { get; init; }
}

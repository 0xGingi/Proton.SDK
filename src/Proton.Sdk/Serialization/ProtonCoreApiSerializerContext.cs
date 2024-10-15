using System.Text.Json.Serialization;
using Proton.Sdk.Addresses;
using Proton.Sdk.Authentication;
using Proton.Sdk.Events;
using Proton.Sdk.Keys;
using Proton.Sdk.Users;

namespace Proton.Sdk.Serialization;

[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(SessionInitiationRequest))]
[JsonSerializable(typeof(SessionInitiationResponse))]
[JsonSerializable(typeof(AuthenticationRequest))]
[JsonSerializable(typeof(AuthenticationResponse))]
[JsonSerializable(typeof(SecondFactorValidationRequest))]
[JsonSerializable(typeof(ScopesResponse))]
[JsonSerializable(typeof(SessionRefreshRequest))]
[JsonSerializable(typeof(SessionRefreshResponse))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(AddressListResponse))]
[JsonSerializable(typeof(AddressResponse))]
[JsonSerializable(typeof(AddressPublicKeyListResponse))]
[JsonSerializable(typeof(ModulusResponse))]
[JsonSerializable(typeof(KeySaltListResponse))]
[JsonSerializable(typeof(LatestEventResponse))]
[JsonSerializable(typeof(EventListResponse))]
internal partial class ProtonCoreApiSerializerContext : JsonSerializerContext
{
    static ProtonCoreApiSerializerContext()
    {
        Default = new ProtonCoreApiSerializerContext(ProtonApiDefaults.GetSerializerOptions());
    }
}

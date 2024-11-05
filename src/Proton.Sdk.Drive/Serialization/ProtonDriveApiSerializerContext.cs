using System.Text.Json.Serialization;
using Proton.Sdk.Drive.Devices;
using Proton.Sdk.Drive.Files;
using Proton.Sdk.Drive.Folders;
using Proton.Sdk.Drive.Links;
using Proton.Sdk.Drive.Shares;
using Proton.Sdk.Drive.Verification;
using Proton.Sdk.Drive.Volumes;
using Proton.Sdk.Events;

namespace Proton.Sdk.Drive.Serialization;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(VolumeListResponse))]
[JsonSerializable(typeof(VolumeCreationParameters))]
[JsonSerializable(typeof(VolumeCreationResponse))]
[JsonSerializable(typeof(VolumeResponse))]
[JsonSerializable(typeof(DeviceListResponse))]
[JsonSerializable(typeof(DeviceCreationParameters))]
[JsonSerializable(typeof(DeviceCreationResponse))]
[JsonSerializable(typeof(DeviceUpdateParameters))]
[JsonSerializable(typeof(ShareResponse))]
[JsonSerializable(typeof(ShareListResponse))]
[JsonSerializable(typeof(LinkResponse))]
[JsonSerializable(typeof(FolderChildListParameters))]
[JsonSerializable(typeof(FolderChildListResponse))]
[JsonSerializable(typeof(FolderCreationParameters))]
[JsonSerializable(typeof(FolderCreationResponse))]
[JsonSerializable(typeof(FileCreationParameters))]
[JsonSerializable(typeof(FileCreationResponse))]
[JsonSerializable(typeof(RevisionCreationParameters))]
[JsonSerializable(typeof(RevisionCreationResponse))]
[JsonSerializable(typeof(RevisionConflictResponse))]
[JsonSerializable(typeof(BlockUploadRequestParameters))]
[JsonSerializable(typeof(BlockRequestResponse))]
[JsonSerializable(typeof(RevisionUpdateParameters))]
[JsonSerializable(typeof(ExtendedAttributes))]
[JsonSerializable(typeof(RevisionResponse))]
[JsonSerializable(typeof(RevisionListResponse))]
[JsonSerializable(typeof(MultipleLinkActionParameters))]
[JsonSerializable(typeof(AggregateResponse<LinkActionResponse>))]
[JsonSerializable(typeof(VerificationInputResponse))]
[JsonSerializable(typeof(MoveLinkParameters))]
[JsonSerializable(typeof(RenameLinkParameters))]
[JsonSerializable(typeof(VolumeEventListResponse))]
[JsonSerializable(typeof(LatestEventResponse))]
[JsonSerializable(typeof(LinkEventDto))]
[JsonSerializable(typeof(DeletedLinkEventDto))]
internal partial class ProtonDriveApiSerializerContext : JsonSerializerContext
{
    static ProtonDriveApiSerializerContext()
    {
        Default = new ProtonDriveApiSerializerContext(ProtonApiDefaults.GetSerializerOptions());
    }
}

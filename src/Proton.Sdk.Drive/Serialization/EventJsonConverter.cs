using System.Text.Json;
using System.Text.Json.Serialization;
using Proton.Sdk.Drive.Volumes;

namespace Proton.Sdk.Drive.Serialization;

internal sealed class EventJsonConverter : JsonConverter<EventDto>
{
    public override EventDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var readerClone = reader;

        try
        {
            // Attempt to deserialize with the clone, if successful, do it with the original, otherwise assume it's the other type
            JsonSerializer.Deserialize(ref readerClone, ProtonDriveApiSerializerContext.Default.LinkEventDto);
        }
        catch
        {
            return JsonSerializer.Deserialize(ref reader, ProtonDriveApiSerializerContext.Default.DeletedLinkEventDto);
        }

        return JsonSerializer.Deserialize(ref reader, ProtonDriveApiSerializerContext.Default.LinkEventDto);
    }

    public override void Write(Utf8JsonWriter writer, EventDto value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}

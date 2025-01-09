using System.Text.Json.Serialization;
using System.Text.Json;

namespace MediathekArr.Converters;

public class StringSanitizerConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.Replace('–', '-') ?? string.Empty;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

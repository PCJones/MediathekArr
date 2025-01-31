using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tvdb.Converters;

/// <summary>
/// <see cref="TimeOnly"/> Converter for TVDB
/// </summary>
public class TimeOnlyConverter(string? serializationFormat) : JsonConverter<TimeOnly?>
{
    private readonly string serializationFormat = serializationFormat ?? dateFormat;
    private const string dateFormat = "HH:mm:ss";

    #region Constructor
    public TimeOnlyConverter() : this(null) { }
    #endregion

    #region Methods
    /// <inheritdoc/>
    public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) return default;
        return TimeOnly.Parse(value!);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.HasValue ? value.Value.ToString(serializationFormat) : string.Empty);
    #endregion
}
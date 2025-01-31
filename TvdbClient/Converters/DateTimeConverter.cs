using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tvdb.Converters;

/// <summary>
/// <see cref="DateTime"/> Converter for TVDB
/// </summary>
public class DateTimeConverter(string? serializationFormat) : JsonConverter<DateTime?>
{
    private readonly string serializationFormat = serializationFormat ?? dateFormat;
    private const string dateFormat = "yyyy-MM-dd HH:mm:ss";

    #region Constructor
    public DateTimeConverter() : this(null) { }
    #endregion

    #region Methods
    /// <inheritdoc/>
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) return default;
        return DateTime.Parse(value!);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.HasValue ? value.Value.ToString(serializationFormat) : string.Empty);
    #endregion
}
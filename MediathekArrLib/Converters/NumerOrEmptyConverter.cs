using System.Text.Json.Serialization;
using System.Text.Json;

namespace MediathekArr.Converters;

public class NumberOrEmptyConverter<T> : JsonConverter<T>
    where T : struct, IConvertible
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null || reader.TokenType == JsonTokenType.String && reader.GetString() == "")
        {
            return default; // Return default value, which will be 0 for int, long, etc.
        }

        // Convert to the target numeric type (int, long, etc.)
        try
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // Handle numeric values directly
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)reader.GetInt32();
                }
                else if (typeof(T) == typeof(long))
                {
                    return (T)(object)reader.GetInt64();
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                // Try parsing string as a number
                string? stringValue = reader.GetString();
                if (!string.IsNullOrEmpty(stringValue))
                {
                    if (typeof(T) == typeof(int) && int.TryParse(stringValue, out int intValue))
                    {
                        return (T)(object)intValue;
                    }
                    else if (typeof(T) == typeof(long) && long.TryParse(stringValue, out long longValue))
                    {
                        return (T)(object)longValue;
                    }

                    // Throw FormatException if we cant parse into any supported number type
                    throw new FormatException($"{stringValue} is not a valid Number");
                }
            }
        }
        catch (Exception ex)
        {
            throw new JsonException($"Error converting value to type {typeof(T)}: {ex.Message}", ex);
        }

        throw new NotSupportedException($"The converter does not support type {typeof(T)}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(Convert.ToDouble(value));
    }
}
using System.Text.Json.Serialization;
using System.Text.Json;

namespace MediathekArr.Utilities
{

    public class NumberOrEmptyConverter<T> : JsonConverter<T>
        where T : struct, IConvertible
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null || (reader.TokenType == JsonTokenType.String && reader.GetString() == ""))
            {
                return default; // Return default value, which will be 0 for int, long, etc.
            }

            // Convert to the target numeric type (int, long, etc.)
            if (typeof(T) == typeof(int))
            {
                return (T)(object)reader.GetInt32();
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)reader.GetInt64();
            }

            throw new NotSupportedException($"The converter does not support type {typeof(T)}.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToDouble(value));
        }
    }
}
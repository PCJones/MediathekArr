using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediathekArr.Converters;

namespace MediathekArr.Converters;

public class NumberOrEmptyConverterUnitTests
{
    [Fact]
    public void Read_NullToken_ReturnsDefault()
    {
        // Arrange
        var converter = new NumberOrEmptyConverter<int>();
        var json = "null";

        // Act
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        var result = converter.Read(ref reader, typeof(int), new JsonSerializerOptions());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Read_EmptyString_ReturnsDefault()
    {
        // Arrange
        var converter = new NumberOrEmptyConverter<int>();
        var json = "\"\"";

        // Act
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        var result = converter.Read(ref reader, typeof(int), new JsonSerializerOptions());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Read_NumberToken_ReturnsNumber()
    {
        // Arrange
        var converter = new NumberOrEmptyConverter<int>();
        var json = "123";

        // Act
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        var result = converter.Read(ref reader, typeof(int), new JsonSerializerOptions());

        // Assert
        result.Should().Be(123);
    }

    [Fact]
    public void Read_StringNumber_ReturnsParsedNumber()
    {
        // Arrange
        var converter = new NumberOrEmptyConverter<int>();
        var json = "\"123\"";

        // Act
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        var result = converter.Read(ref reader, typeof(int), new JsonSerializerOptions());

        // Assert
        result.Should().Be(123);
    }

    [Fact]
    public void Read_InvalidString_ThrowsJsonException()
    {
        // Arrange
        var converter = new NumberOrEmptyConverter<int>();
        var json = "\"abc\"";

        // Act
        Action act = () =>
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            reader.Read();
            converter.Read(ref reader, typeof(int), new JsonSerializerOptions());
        };

        // Assert
        var caughtException = act.Should().ThrowExactly<JsonException>();
    }

    [Fact]
    public void Read_InvalidType_ThrowsJsonException()
    {
        // Arrange
        var converter = new NumberOrEmptyConverter<DateTime>();
        var json = "123";

        // Act
        Action act = () =>
        {
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
            reader.Read();
            converter.Read(ref reader, typeof(int), new JsonSerializerOptions());
        };

        // Assert
        var caughtException = act.Should().ThrowExactly<NotSupportedException>();
    }

    [Fact]
    public void Write_Number_WritesNumberValue()
    {
        // Arrange
        var converter = new NumberOrEmptyConverter<int>();
        using var stream = new MemoryStream();

        // Act
        using var writer = new Utf8JsonWriter(stream);
        converter.Write(writer, 123, new JsonSerializerOptions());
        writer.Flush();
        var json = Encoding.UTF8.GetString(stream.ToArray());

        // Assert
        json.Should().Be("123");
    }
}
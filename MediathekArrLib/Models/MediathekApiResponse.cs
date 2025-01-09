using System.Text.Json.Serialization;

namespace MediathekArr.Models;

public class MediathekApiResponse
{
    [JsonPropertyName("result")]
    public MediathekApiResult Result { get; set; }

    [JsonPropertyName("err")]
    public object? Err { get; set; }
}

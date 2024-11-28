using MediathekArrLib.Utilities;
using System.Text.Json.Serialization;

namespace MediathekArrLib.Models;

public class ApiResultItem
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("duration")]
    [JsonConverter(typeof(NumberOrEmptyConverter<int>))]
    public int Duration { get; set; }

    [JsonPropertyName("size")]
    [JsonConverter(typeof(NumberOrEmptyConverter<long>))]
    public long Size { get; set; }

    [JsonPropertyName("url_website")]
    public string UrlWebsite { get; set; }

    [JsonPropertyName("url_video")]
    public string UrlVideo { get; set; }

    [JsonPropertyName("url_video_low")]
    public string UrlVideoLow { get; set; }

    [JsonPropertyName("url_video_hd")]
    public string UrlVideoHd { get; set; }
}

﻿using MediathekArr.Converters;
using System.Text.Json.Serialization;

namespace MediathekArr.Models;

public class ApiResultItem
{
    [JsonPropertyName("channel")]
    public string Channel { get; set; }

    [JsonPropertyName("topic")]
    [JsonConverter(typeof(StringSanitizerConverter))]
    public string Topic { get; set; }

    [JsonPropertyName("title")]
    [JsonConverter(typeof(StringSanitizerConverter))]
    public string Title { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("filmlisteTimestamp")]
    [JsonConverter(typeof(NumberOrEmptyConverter<long>))]
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
    [JsonPropertyName("url_subtitle")]
    public string UrlSubtitle { get; set; }
    [JsonIgnore]
    public string Language => Title?.Contains("(Englisch)") ?? false ? "ENGLISH" : "GERMAN";
}

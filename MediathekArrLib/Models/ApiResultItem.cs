using MediathekArrLib.Utilities;
using System.Text.Json.Serialization;

namespace MediathekArrLib.Models;

public class ApiResultItem
{
    [JsonIgnore]
    private readonly Dictionary<string, string> LanguageIdentifiers = new()
    {
        { "(originalversion", "ORIGINAL" },
        { "(englisch", "ENGLISH" },
        { "(english version)", "ENGLISH" },
        { "(english)", "ENGLISH" },
    };

    [JsonIgnore]
    private readonly string[] BurnedInSubtitleIdentifiers = ["originalversion mit untertitel"];

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
    public string Language => HasBurnedInSubtitles 
        ? "GERMAN.SUBBED.HC" 
        : LanguageIdentifiers.FirstOrDefault(x => Title.Contains(x.Key, StringComparison.CurrentCultureIgnoreCase)).Value 
        ?? "GERMAN";

    [JsonIgnore]
    private bool HasBurnedInSubtitles => BurnedInSubtitleIdentifiers.Any(Title.ToLower().Contains) && string.IsNullOrEmpty(UrlSubtitle);
}

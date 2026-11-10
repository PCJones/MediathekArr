using System.Text.Json.Serialization;

namespace MediathekArr.Models.Rulesets;

public class Ruleset
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("mediaId")]
    public int? MediaId { get; set; }

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty; // Raw topic string from API

    [JsonIgnore]
    public List<string> Topics
    {
        get
        {
            return Topic.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
    }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    private List<Filter> _filters = [];
    private List<TitleRegexRule> _titleRegexRules = [];

    // The API may send null for these lists
    [JsonPropertyName("filters")]
    public List<Filter> Filters { get => _filters; set => _filters = value ?? []; }

    [JsonPropertyName("titleRegexRules")]
    public List<TitleRegexRule> TitleRegexRules { get => _titleRegexRules; set => _titleRegexRules = value ?? []; }

    [JsonPropertyName("episodeRegex")]
    public string? EpisodeRegex { get; set; }

    [JsonPropertyName("seasonRegex")]
    public string? SeasonRegex { get; set; }

    [JsonPropertyName("matchingStrategy")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MatchingStrategy MatchingStrategy { get; set; }

    [JsonPropertyName("media")]
    public Media? Media { get; set; }
}

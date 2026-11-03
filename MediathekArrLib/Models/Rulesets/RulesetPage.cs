using System.Text.Json.Serialization;

namespace MediathekArr.Models.Rulesets;

public class RulesetPage
{
    [JsonPropertyName("rulesets")]
    public List<Ruleset> Rulesets { get; set; } = [];

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}

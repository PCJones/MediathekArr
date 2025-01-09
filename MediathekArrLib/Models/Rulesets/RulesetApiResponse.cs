using System.Text.Json.Serialization;

namespace MediathekArr.Models.Rulesets;

public class RulesetApiResponse
{
    [JsonPropertyName("rulesets")]
    public List<Ruleset> Rulesets { get; set; } = [];

    [JsonPropertyName("pagination")]
    public Pagination Pagination { get; set; } = new();
}

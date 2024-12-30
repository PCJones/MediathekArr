using System.Text.Json.Serialization;

namespace MediathekArrLib.Models.Rulesets;

public class RulesetApiResponse
{
    [JsonPropertyName("rulesets")]
    public List<Ruleset> Rulesets { get; set; } = [];

    [JsonPropertyName("pagination")]
    public Pagination Pagination { get; set; } = new();
}

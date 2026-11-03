using System.Text.Json.Serialization;

namespace MediathekArr.Models.Rulesets;

public class RulesetApiResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public RulesetPage? Data { get; set; }
}

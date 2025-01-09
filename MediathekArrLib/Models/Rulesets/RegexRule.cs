using System.Text.Json.Serialization;

namespace MediathekArr.Models.Rulesets;

public class RegexRule
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("pattern")]
    public string Pattern { get; set; } = string.Empty;
}

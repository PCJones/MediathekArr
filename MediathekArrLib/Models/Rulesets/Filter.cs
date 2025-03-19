using System.Text.Json.Serialization;

namespace MediathekArr.Models.Rulesets;

public class Filter
{
    [JsonPropertyName("attribute")]
    public string Attribute { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MatchType Type { get; set; }

    [JsonPropertyName("value")]
    public object Value { get; set; } = string.Empty;
}

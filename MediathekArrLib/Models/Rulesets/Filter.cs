using System.Text.Json.Serialization;

namespace MediathekArrLib.Models.Rulesets
{
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

    public enum MatchType
    {
        ExactMatch,
        Contains,
        Regex,
        GreaterThan,
        LessThan
    }
}

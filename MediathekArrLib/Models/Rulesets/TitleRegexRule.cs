using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MediathekArr.Models.Rulesets;

public class TitleRegexRule
{
    [JsonPropertyName("type")]
    public TitleRegexRuleType Type { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; } // For static text

    [JsonPropertyName("field")]
    public string? Field { get; set; } // API field to extract from

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; } // Regex pattern
}

using System.Text.Json.Serialization;

namespace MediathekArr.Models.Rulesets;

public class Media
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tvdbId")]
    public int? TvdbId { get; set; }
}

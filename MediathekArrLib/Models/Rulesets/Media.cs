using System.Text.Json.Serialization;

namespace MediathekArr.Models.Rulesets;

public class Media
{
    [JsonPropertyName("media_id")]
    public int Id { get; set; }

    [JsonPropertyName("media_name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("media_type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("media_tmdbId")]
    public int? TmdbId { get; set; }

    [JsonPropertyName("media_imdbId")]
    public string? ImdbId { get; set; }

    [JsonPropertyName("media_tvdbId")]
    public int? TvdbId { get; set; }
}

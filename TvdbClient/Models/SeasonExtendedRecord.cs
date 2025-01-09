using System.Text.Json.Serialization;

namespace Tvdb.Models;

/// <summary>
/// extended season record
/// </summary>
public class SeasonExtendedRecord : SeasonBaseRecord
{

    [JsonPropertyName("artwork")]
    public ICollection<ArtworkBaseRecord> Artwork { get; set; }

    [JsonPropertyName("episodes")]
    public ICollection<EpisodeBaseRecord> Episodes { get; set; }

    [JsonPropertyName("trailers")]
    public ICollection<Trailer> Trailers { get; set; }

    [JsonPropertyName("tagOptions")]
    public ICollection<TagOption> TagOptions { get; set; }

    [JsonPropertyName("translations")]
    public ICollection<Translation> Translations { get; set; }
}
using System.Text.Json.Serialization;

namespace Tvdb.Models;

/// <summary>
/// extended episode record
/// </summary>
public class EpisodeExtendedRecord : EpisodeBaseRecord
{
    [JsonPropertyName("awards")]
    public ICollection<AwardBaseRecord> Awards { get; set; }

    [JsonPropertyName("characters")]
    public ICollection<Character> Characters { get; set; }

    [JsonPropertyName("companies")]
    public ICollection<Company> Companies { get; set; }

    [JsonPropertyName("contentRatings")]
    public ICollection<ContentRating> ContentRatings { get; set; }

    [JsonPropertyName("networks")]
    public ICollection<Company> Networks { get; set; }

    [JsonPropertyName("nominations")]
    public ICollection<AwardNomineeBaseRecord> Nominations { get; set; }

    [JsonPropertyName("productionCode")]
    public string ProductionCode { get; set; }

    [JsonPropertyName("remoteIds")]
    public ICollection<RemoteID> RemoteIds { get; set; }

    [JsonPropertyName("studios")]
    public ICollection<Company> Studios { get; set; }

    [JsonPropertyName("tagOptions")]
    public ICollection<TagOption> TagOptions { get; set; }

    [JsonPropertyName("trailers")]
    public ICollection<Trailer> Trailers { get; set; }

    [JsonPropertyName("translations")]
    public TranslationExtended Translations { get; set; }
}
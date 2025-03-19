using System.Text.Json.Serialization;

namespace Tvdb.Models;

/// <summary>
/// The extended record for a series. All series airs time like firstAired, lastAired, nextAired, etc. are in US EST for US series, and for all non-US series, the time of the show’s country capital or most populous city. For streaming services, is the official release time. See https://support.thetvdb.com/kb/faq.php?id=29.
/// </summary>
public class SeriesExtendedRecord : SeriesBaseRecord
{

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; }

    [JsonPropertyName("airsDays")]
    public SeriesAirsDays AirsDays { get; set; }

    [JsonConverter(typeof(Converters.TimeOnlyConverter))]
    [JsonPropertyName("airsTime")]
    public TimeOnly? AirsTime { get; set; }

    [JsonPropertyName("artworks")]
    public ICollection<ArtworkExtendedRecord> Artworks { get; set; }


    [JsonPropertyName("characters")]
    public ICollection<Character> Characters { get; set; }

    [JsonPropertyName("contentRatings")]
    public ICollection<ContentRating> ContentRatings { get; set; }

    [JsonPropertyName("lists")]
    public object Lists { get; set; }

    [JsonPropertyName("genres")]
    public ICollection<GenreBaseRecord> Genres { get; set; }

    [JsonPropertyName("companies")]
    public ICollection<Company> Companies { get; set; }

    [JsonPropertyName("originalNetwork")]
    public Company OriginalNetwork { get; set; }

    [JsonPropertyName("overview")]
    public string Overview { get; set; }

    [JsonPropertyName("latestNetwork")]
    public Company LatestNetwork { get; set; }

    [JsonPropertyName("remoteIds")]
    public ICollection<RemoteID> RemoteIds { get; set; }

    [JsonPropertyName("seasons")]
    public ICollection<SeasonBaseRecord> Seasons { get; set; }

    [JsonPropertyName("seasonTypes")]
    public ICollection<SeasonType> SeasonTypes { get; set; }

    [JsonPropertyName("tags")]
    public ICollection<TagOption> Tags { get; set; }

    [JsonPropertyName("trailers")]
    public ICollection<Trailer> Trailers { get; set; }

    [JsonPropertyName("translations")]
    public TranslationExtended Translations { get; set; }
}
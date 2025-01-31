using System.Text.Json.Serialization;

namespace Tvdb.Models;

/// <summary>
/// extended movie record
/// </summary>
public class MovieExtendedRecord : MovieBaseRecord
{
    [JsonPropertyName("artworks")]
    public ICollection<ArtworkBaseRecord> Artworks { get; set; }

    [JsonPropertyName("audioLanguages")]
    public ICollection<string> AudioLanguages { get; set; }

    [JsonPropertyName("awards")]
    public ICollection<AwardBaseRecord> Awards { get; set; }

    [JsonPropertyName("boxOffice")]
    public string BoxOffice { get; set; }

    [JsonPropertyName("boxOfficeUS")]
    public string BoxOfficeUS { get; set; }

    [JsonPropertyName("budget")]
    public string Budget { get; set; }

    [JsonPropertyName("characters")]
    public ICollection<Character> Characters { get; set; }

    [JsonPropertyName("companies")]
    public Companies Companies { get; set; }

    [JsonPropertyName("contentRatings")]
    public ICollection<ContentRating> ContentRatings { get; set; }

    [JsonPropertyName("first_release")]
    public Release First_release { get; set; }

    [JsonPropertyName("genres")]
    public ICollection<GenreBaseRecord> Genres { get; set; }

    [JsonPropertyName("inspirations")]
    public ICollection<Inspiration> Inspirations { get; set; }

    [JsonConverter(typeof(Converters.DateTimeConverter))]

    [JsonPropertyName("lists")]
    public ICollection<ListBaseRecord> Lists { get; set; }

    [JsonPropertyName("originalCountry")]
    public string OriginalCountry { get; set; }

    [JsonPropertyName("originalLanguage")]
    public string OriginalLanguage { get; set; }

    [JsonPropertyName("production_countries")]
    public ICollection<ProductionCountry> Production_countries { get; set; }

    [JsonPropertyName("releases")]
    public ICollection<Release> Releases { get; set; }

    [JsonPropertyName("remoteIds")]
    public ICollection<RemoteID> RemoteIds { get; set; }

    [JsonPropertyName("spoken_languages")]
    public ICollection<string> Spoken_languages { get; set; }

    [JsonPropertyName("studios")]
    public ICollection<StudioBaseRecord> Studios { get; set; }

    [JsonPropertyName("subtitleLanguages")]
    public ICollection<string> SubtitleLanguages { get; set; }

    [JsonPropertyName("tagOptions")]
    public ICollection<TagOption> TagOptions { get; set; }

    [JsonPropertyName("trailers")]
    public ICollection<Trailer> Trailers { get; set; }

    [JsonPropertyName("translations")]
    public TranslationExtended Translations { get; set; }
}
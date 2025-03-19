using System.Text.Json.Serialization;

namespace Tvdb.Models;

/// <summary>
/// The base record for a series. All series airs time like firstAired, lastAired, nextAired, etc. are in US EST for US series, and for all non-US series, the time of the show’s country capital or most populous city. For streaming services, is the official release time. See https://support.thetvdb.com/kb/faq.php?id=29.
/// </summary>
public class SeriesBaseRecord : AbstractBaseRecord
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("aliases")]
    public ICollection<Alias> Aliases { get; set; }

    [JsonPropertyName("averageRuntime")]
    public int? AverageRuntime { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("defaultSeasonType")]
    public long? DefaultSeasonType { get; set; }

    [JsonPropertyName("episodes")]
    public ICollection<EpisodeBaseRecord> Episodes { get; set; }

    [JsonConverter(typeof(Converters.DateOnlyConverter))]
    [JsonPropertyName("firstAired")]
    public DateOnly? FirstAired { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }

    [JsonPropertyName("isOrderRandomized")]
    public bool? IsOrderRandomized { get; set; }

    [JsonConverter(typeof(Converters.DateOnlyConverter))]
    [JsonPropertyName("lastAired")]
    public DateOnly? LastAired { get; set; }

    [JsonConverter(typeof(Converters.DateTimeConverter))]

    [JsonPropertyName("lastUpdated")]
    public DateTime? LastUpdated { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("nameTranslations")]
    public ICollection<string> NameTranslations { get; set; }

    [JsonConverter(typeof(Converters.DateOnlyConverter))]
    [JsonPropertyName("nextAired")]
    public DateOnly? NextAired { get; set; }

    [JsonPropertyName("originalCountry")]
    public string OriginalCountry { get; set; }

    [JsonPropertyName("originalLanguage")]
    public string OriginalLanguage { get; set; }

    [JsonPropertyName("overviewTranslations")]
    public ICollection<string> OverviewTranslations { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }

    [JsonPropertyName("status")]
    public Status Status { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tvdb.Models;

/// <summary>
/// base episode record
/// </summary>
public class EpisodeBaseRecord : AbstractBaseRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("absoluteNumber")]
    public int? AbsoluteNumber { get; set; }

    [JsonPropertyName("aired")]
    public DateTime Aired { get; set; }

    [JsonPropertyName("airsAfterSeason")]
    public int? AirsAfterSeason { get; set; }

    [JsonPropertyName("airsBeforeEpisode")]
    public int? AirsBeforeEpisode { get; set; }

    [JsonPropertyName("airsBeforeSeason")]
    public int? AirsBeforeSeason { get; set; }

    /// <summary>
    /// season, midseason, or series
    /// </summary>

    [JsonPropertyName("finaleType")]
    public string FinaleType { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }

    [JsonPropertyName("imageType")]
    public int? ImageType { get; set; }

    [JsonPropertyName("isMovie")]
    public int? IsMovie { get; set; }

    [JsonConverter(typeof(Converters.DateTimeConverter))]
    [JsonPropertyName("lastUpdated")]
    public DateTime? LastUpdated { get; set; }

    [JsonPropertyName("linkedMovie")]
    public int? LinkedMovie { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("nameTranslations")]
    public ICollection<string> NameTranslations { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("overview")]
    public string Overview { get; set; }

    [JsonPropertyName("overviewTranslations")]
    public ICollection<string> OverviewTranslations { get; set; }

    [JsonPropertyName("runtime")]
    public int Runtime { get; set; }

    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("seasons")]
    public ICollection<SeasonBaseRecord> Seasons { get; set; }

    [JsonPropertyName("seriesId")]
    public long? SeriesId { get; set; }

    [JsonPropertyName("seasonName")]
    public string SeasonName { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; }
}
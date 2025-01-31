using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tvdb.Models;

/// <summary>
/// season genre record
/// </summary>
public class SeasonBaseRecord : AbstractBaseRecord
{

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }

    [JsonPropertyName("imageType")]
    public int? ImageType { get; set; }

    [JsonConverter(typeof(Converters.DateTimeConverter))]

    [JsonPropertyName("lastUpdated")]
    public DateTime? LastUpdated { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("nameTranslations")]
    public ICollection<string> NameTranslations { get; set; }

    [JsonPropertyName("number")]
    public long? Number { get; set; }

    [JsonPropertyName("overviewTranslations")]
    public ICollection<string> OverviewTranslations { get; set; }

    [JsonPropertyName("companies")]
    public Companies Companies { get; set; }

    [JsonPropertyName("seriesId")]
    public long? SeriesId { get; set; }

    [JsonPropertyName("type")]
    public SeasonType Type { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; }
}

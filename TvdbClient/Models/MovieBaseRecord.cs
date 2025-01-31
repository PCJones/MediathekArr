using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Tvdb.Models;
/// <summary>
/// base movie record
/// </summary>
public class MovieBaseRecord : AbstractBaseRecord
{

    [JsonPropertyName("aliases")]
    public ICollection<Alias> Aliases { get; set; }

    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }

    [JsonConverter(typeof(Converters.DateTimeConverter))]

    [JsonPropertyName("lastUpdated")]
    public DateTime? LastUpdated { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("nameTranslations")]
    public ICollection<string> NameTranslations { get; set; }

    [JsonPropertyName("overviewTranslations")]
    public ICollection<string> OverviewTranslations { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }

    [JsonPropertyName("status")]
    public Status Status { get; set; }

    [JsonPropertyName("runtime")]
    public int? Runtime { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; }
}
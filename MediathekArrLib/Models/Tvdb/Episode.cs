using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MediathekArr.Models.Tvdb;

public class Episode
{
    [Key]
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public string? Name { get; set; }
    public DateTime? Aired { get; set; }
    public int? Runtime { get; set; }
    public int? SeasonNumber { get; set; }
    public int? EpisodeNumber { get; set; }

    [ForeignKey("SeriesId")]
    [JsonIgnore]
    public Series SeriesCache { get; set; }

    [JsonIgnore]
    public string PaddedSeason => SeasonNumber.Value.ToString("D2") ?? string.Empty;
    [JsonIgnore]
    public string PaddedEpisode => EpisodeNumber.Value.ToString("D2") ?? string.Empty;
}
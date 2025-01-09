using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MediathekArr.Infrastructure;

public class Episode
{
    [Key]
    public int Id { get; set; }
    public int SeriesId { get; set; }
    public string Name { get; set; }
    public DateTime Aired { get; set; }
    public int Runtime { get; set; }
    public int SeasonNumber { get; set; }
    public int EpisodeNumber { get; set; }

    [ForeignKey("SeriesId")]
    public Series SeriesCache { get; set; }
}
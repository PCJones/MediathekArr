namespace MediathekArrLib.Models;

public record TvdbEpisode(string Name, DateTime? Aired, int? Runtime, int SeasonNumber, int EpisodeNumber)
{
    public string PaddedSeason => SeasonNumber.ToString("D2");
    public string PaddedEpisode => EpisodeNumber.ToString("D2");
};

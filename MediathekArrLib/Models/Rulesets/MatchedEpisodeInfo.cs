namespace MediathekArrLib.Models.Rulesets
{
    public record MatchedEpisodeInfo(TvdbEpisode Episode, ApiResultItem Item, string ShowName, string MatchedTitle, EpisodeType episodeType);
}

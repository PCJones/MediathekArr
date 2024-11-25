namespace MediathekArrLib.Models.Rulesets
{
    public record IdentificationResult(string UsedRuleset, string Name, string GermanName, int? SeasonNumber, int? EpisodeNumber, string ItemTitle, TvdbEpisode MatchedEpisode);
}

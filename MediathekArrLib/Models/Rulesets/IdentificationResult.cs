using MediathekArr.Models.Tvdb;

namespace MediathekArr.Models.Rulesets;

public record IdentificationResult(string UsedRuleset, string Name, string GermanName, int? SeasonNumber, int? EpisodeNumber, string ItemTitle, Episode MatchedEpisode);
using MediathekArrLib.Models.Tvdb;

namespace MediathekArrLib.Models.Rulesets;

public record MatchedEpisodeInfo(Episode Episode, ApiResultItem Item, string ShowName, string MatchedTitle);

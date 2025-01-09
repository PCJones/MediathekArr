using MediathekArr.Models.Tvdb;

namespace MediathekArr.Models.Rulesets;

public record MatchedEpisodeInfo(Episode Episode, ApiResultItem Item, string ShowName, string MatchedTitle);

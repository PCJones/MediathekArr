namespace MediathekArr.Models.Rulesets;

public enum MatchingStrategy
{
    SeasonAndEpisodeNumber, // Use season + episode number for matching
    ByAbsoluteEpisodeNumber, // Use absolute episode number for matching // TODO rename this to AbsoluteEpisodeNumber once API is c#
    ItemTitleIncludes,      // Match episodes where the tvdb episode name contains this title
    ItemTitleExact,          // Match episodes with an exact itemTitle
    ItemTitleEqualsAirdate
}

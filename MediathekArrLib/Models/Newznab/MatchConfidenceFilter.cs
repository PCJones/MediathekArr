namespace MediathekArr.Models.Newznab;

public record MatchConfidenceFilter(bool HideNoMatch, bool HideUncertainMatch)
{
    public bool IsAllowed(Item item) => item.MatchConfidence switch
    {
        MatchConfidence.NoMatch => !HideNoMatch,
        MatchConfidence.Uncertain => !HideUncertainMatch,
        _ => true
    };

    public string CacheKey => $"{HideNoMatch}_{HideUncertainMatch}";
}

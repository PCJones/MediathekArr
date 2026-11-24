using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MediathekArr.Models;
using MediathekArr.Models.Newznab;
using MediathekArr.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Guid = MediathekArr.Models.Newznab.Guid;

namespace MediathekArr.Services;

/// <summary>
/// Movie search (t=movie). Matches Mediathek items against the known titles of a movie
/// and returns them with Radarr-parseable names ("Title.Year.GERMAN.1080p.WEB.h264-MEDiATHEK")
/// and the newznab "imdb" attribute, so Radarr can map them to the searched movie.
/// </summary>
public partial class MediathekSearchService
{
    private const int MovieMinDurationSeconds = 50 * 60;

    public async Task<string> FetchMovieSearchResults(MovieLookupService.MovieInfo? movieInfo, string? q, int limit, int offset, MatchConfidenceFilter matchConfidenceFilter)
    {
        List<string> titles;
        int? year;
        string? imdbId = movieInfo?.ImdbId;

        if (movieInfo is not null)
        {
            titles = movieInfo.Titles;
            year = movieInfo.Year;
        }
        else if (!string.IsNullOrWhiteSpace(q))
        {
            (var title, year) = SplitTitleAndYear(q);
            titles = [title];
        }
        else
        {
            return NewznabUtils.SerializeRss(NewznabUtils.GetEmptyRssResult());
        }

        var cacheKey = $"movie_{imdbId ?? "null"}_{string.Join("|", titles)}_{year}_{limit}_{offset}";
        if (_cache.TryGetValue(cacheKey, out string? cachedResponse))
        {
            return cachedResponse ?? "";
        }

        var items = new List<Item>();
        var seenUrls = new HashSet<string>();

        foreach (var title in titles)
        {
            var queries = new List<object> { new { fields = _queryFields, query = title } };
            var results = await FetchMediathekViewApiResponseAsync(queries, 1000);

            foreach (var result in results)
            {
                // films only: skip short items and anything carrying an episode/part marker
                if (ShouldSkipItem(result) || result.Duration < MovieMinDurationSeconds || EpisodeMarkerRegex().IsMatch(result.Title))
                {
                    continue;
                }

                if (!MovieTitleMatches(result, title, year))
                {
                    continue;
                }

                if (!seenUrls.Add(result.UrlWebsite + "|" + result.Title))
                {
                    continue;
                }

                items.AddRange(GenerateMovieRssItems(result, titles[0], year, imdbId));
            }
        }

        _logger.LogInformation("Movie search {Titles} ({Year}): {Count} releases", string.Join(" / ", titles), year, items.Count);

        var response = ConvertNewznabItemsToRss(items, limit, offset, matchConfidenceFilter);
        _cache.Set(cacheKey, response, _cacheTimeSpan);
        return response;
    }

    /// <summary>"Toni Erdmann 2016" → ("Toni Erdmann", 2016)</summary>
    private static (string title, int? year) SplitTitleAndYear(string q)
    {
        var match = TrailingYearRegex().Match(q.Trim());
        if (match.Success && int.TryParse(match.Groups[2].Value, out var y))
        {
            return (match.Groups[1].Value.Trim(), y);
        }
        return (q.Trim(), null);
    }

    /// <summary>
    /// Item title must be the movie title, optionally followed by a descriptor after a separator
    /// ("Toni Erdmann - Spielfilm, Deutschland 2016", "Cash Truck - Montagskino Adrenalin").
    /// "Der Untergang des Römischen Reiches" does NOT match "Der Untergang".
    /// If the item mentions a production year, it must be within one year of the movie's year.
    /// </summary>
    private static bool MovieTitleMatches(ApiResultItem item, string movieTitle, int? year)
    {
        var wanted = NormalizeForCompare(movieTitle);
        if (wanted.Length == 0)
        {
            return false;
        }

        var itemTitle = ParenthesisSuffixRegex().Replace(item.Title, "").Trim();

        bool matches = NormalizeForCompare(itemTitle) == wanted
                       || NormalizeForCompare(item.Topic) == wanted;
        string descriptor = string.Empty; // text after the title, e.g. "Spielfilm, Deutschland 2016"

        if (!matches)
        {
            foreach (Match sep in TitleSeparatorRegex().Matches(itemTitle))
            {
                if (NormalizeForCompare(itemTitle[..sep.Index]) == wanted)
                {
                    matches = true;
                    descriptor = itemTitle[(sep.Index + sep.Length)..];
                    break;
                }
            }
        }

        if (!matches)
        {
            return false;
        }

        // only look for a production year in the descriptor, so titles like "1917" don't trip it
        if (year is not null && descriptor.Length > 0)
        {
            var yearMatch = ProductionYearRegex().Match(descriptor);
            if (yearMatch.Success && int.TryParse(yearMatch.Value, out var itemYear) && Math.Abs(itemYear - year.Value) > 1)
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeForCompare(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return string.Empty;
        }

        s = s.ToLowerInvariant().Replace("&", " und ").Replace("ß", "ss");
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return WhitespaceCollapseRegex().Replace(sb.ToString(), " ").Trim();
    }

    private static List<Item> GenerateMovieRssItems(ApiResultItem item, string movieTitle, int? year, string? imdbId)
    {
        var items = new List<Item>();

        if (!string.IsNullOrEmpty(item.UrlVideoHd))
        {
            items.Add(CreateMovieRssItem(item, movieTitle, year, imdbId, "1080p", 1.75, "Movies > HD", ["2000", "2040"], item.UrlVideoHd));
        }

        if (!string.IsNullOrEmpty(item.UrlVideo))
        {
            items.Add(CreateMovieRssItem(item, movieTitle, year, imdbId, "720p", 1.0, "Movies > HD", ["2000", "2040"], item.UrlVideo));
        }

        if (!string.IsNullOrEmpty(item.UrlVideoLow))
        {
            items.Add(CreateMovieRssItem(item, movieTitle, year, imdbId, "480p", 0.4, "Movies > SD", ["2000", "2030"], item.UrlVideoLow));
        }

        return items;
    }

    private static Item CreateMovieRssItem(ApiResultItem item, string movieTitle, int? year, string? imdbId, string quality, double sizeMultiplier, string category, string[] categoryValues, string url)
    {
        var adjustedSize = (long)(item.Size * sizeMultiplier);

        // Same m3u8 minimum size estimate as for episodes
        if (url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
        {
            long bitrate = quality switch { "1080p" => 750000, "720p" => 450000, "480p" => 250000, _ => 600000 };
            long estimatedMinSize = (long)item.Duration * bitrate;
            if (adjustedSize < estimatedMinSize)
            {
                adjustedSize = estimatedMinSize;
            }
        }

        if (!string.IsNullOrEmpty(item.UrlSubtitle))
        {
            adjustedSize += 15000000;
        }

        var rawTitle = $"{movieTitle}{(year is null ? "" : $" {year}")}.{item.Language}.{quality}.WEB.h264-MEDiATHEK";
        var formattedTitle = FormatTitle(rawTitle);

        var encodedTitle = Convert.ToBase64String(Encoding.UTF8.GetBytes(formattedTitle));
        var encodedVideoUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedSubtitleUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.UrlSubtitle ?? ""));
        var fakeDownloadUrl = $"/api/fake_nzb_download?encodedVideoUrl={encodedVideoUrl}&encodedSubtitleUrl={encodedSubtitleUrl}&encodedTitle={encodedTitle}";

        var attributes = new List<Models.Newznab.Attribute>();
        foreach (var categoryValue in categoryValues)
        {
            attributes.Add(new Models.Newznab.Attribute { Name = "category", Value = categoryValue });
        }
        if (imdbId is not null)
        {
            attributes.Add(new Models.Newznab.Attribute { Name = "imdb", Value = imdbId[2..] });
        }
        if (!string.IsNullOrEmpty(item.UrlSubtitle))
        {
            attributes.Add(new Models.Newznab.Attribute { Name = "subs", Value = "German" });
        }

        return new Item
        {
            Title = formattedTitle,
            Guid = new Guid
            {
                IsPermaLink = true,
                Value = $"{item.UrlWebsite}#{quality}-movie-{item.Language}",
            },
            Link = url,
            Comments = item.UrlWebsite,
            PubDate = DateTimeOffset.FromUnixTimeSeconds(item.Timestamp).ToString("R"),
            Category = category,
            Description = item.Description,
            Enclosure = new Enclosure
            {
                Url = fakeDownloadUrl,
                Length = adjustedSize,
                Type = "application/x-nzb"
            },
            Attributes = attributes
        };
    }

    [GeneratedRegex(@"^(.*?)[\s\.\(]+((?:19|20)\d{2})\)?$")]
    private static partial Regex TrailingYearRegex();

    // trailing "(Englisch)", "(mit Untertitel)", "(Audiodeskription)", "(Originalversion …)" – possibly several
    [GeneratedRegex(@"(\s*\([^)]*\))+\s*$")]
    private static partial Regex ParenthesisSuffixRegex();

    [GeneratedRegex(@"\s+[-–|]\s+|:\s+")]
    private static partial Regex TitleSeparatorRegex();

    [GeneratedRegex(@"(?<!\d)(19|20)\d{2}(?!\d)")]
    private static partial Regex ProductionYearRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceCollapseRegex();

    // "(S02/E03)", "Folge 4", "Staffel 2", "(1/2)"
    [GeneratedRegex(@"S\d{1,4}\s*/\s*E\d{1,4}|\bFolge\s*\d+|\bStaffel\s*\d+|\(\d+/\d+\)", RegexOptions.IgnoreCase)]
    private static partial Regex EpisodeMarkerRegex();
}

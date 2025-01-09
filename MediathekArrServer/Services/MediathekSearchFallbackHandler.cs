using MediathekArrLib.Models;
using MediathekArrLib.Models.Newznab;
using MediathekArrLib.Utilities;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Guid = MediathekArrLib.Models.Newznab.Guid;

namespace MediathekArrServer.Services;

public partial class MediathekSearchFallbackHandler
{
    public static List<Item> GetFallbackSearchResultItemsById(string? apiResponse, TvdbEpisode episode, TvdbData tvdbData)
    {
        if (string.IsNullOrWhiteSpace(apiResponse))
        {
            return [];
        }

        var filteredResponse = ApplyFilters(apiResponse, episode);
        var seasonNumber = episode.SeasonNumber.ToString();
        var episodeNumber = episode.EpisodeNumber.ToString();
        return filteredResponse?.Result.Results.SelectMany(item => GenerateRssItems(item, seasonNumber, episodeNumber, tvdbData)).ToList() ?? [];
    }

    public static List<Item> GetFallbackSearchResultItemsByString(List<ApiResultItem>? unmatchedFilteredResultItems, string? season)
    {
        if (unmatchedFilteredResultItems is null || unmatchedFilteredResultItems.Count == 0)
        {
            return [];
        }

        return unmatchedFilteredResultItems.SelectMany(item => GenerateRssItems(item, season, null)).ToList() ?? [];
    }


    private static List<Item> GenerateRssItems(ApiResultItem item, string? season, string? episode, TvdbData? tvdbData = null)
    {
        var items = new List<Item>();

        string[] categories = ["5000", "2000"];

        if (!string.IsNullOrEmpty(item.UrlVideoHd))
        {
            items.AddRange(CreateRssItems(item, season, episode, tvdbData, "1080p", 1.75, "TV > HD", [.. categories, "5040", "2040"], item.UrlVideoHd));
        }

        if (!string.IsNullOrEmpty(item.UrlVideo))
        {
            items.AddRange(CreateRssItems(item, season, episode, tvdbData, "720p", 1.0, "TV > HD", [.. categories, "5040", "2040"], item.UrlVideo));
        }

        if (!string.IsNullOrEmpty(item.UrlVideoLow))
        {
            items.AddRange(CreateRssItems(item, season, episode, tvdbData, "480p", 0.4, "TV > SD", [.. categories, "5030", "2030"], item.UrlVideoLow));

        }

        return items;
    }

    private static List<Item> CreateRssItems(ApiResultItem item, string? season, string? episode, TvdbData? tvdbData, string quality, double sizeMultiplier, string category, string[] categoryValues, string url)
    {
        var items = new List<Item>();

        // Generate title with season and formatted date
        var formattedDate = ExtractDate(item.Title);

        // Create two items if both season and formatted date are present
        if (!string.IsNullOrEmpty(formattedDate))
        {
            // Title with formattedDate in it
            if (!string.IsNullOrEmpty(formattedDate))
            {
                items.Add(CreateRssItem(item, formattedDate.Split('-')[0], null, episode, tvdbData, quality, sizeMultiplier, category, categoryValues, url, formattedDate));
            }
        }

        items.Add(CreateRssItem(item, null, season, episode, tvdbData, quality, sizeMultiplier, category, categoryValues, url));

        return items;
    }

    private static Item CreateRssItem(ApiResultItem item, string? yearSeason, string? season, string? episode, TvdbData? tvdbData, string quality, double sizeMultiplier, string category, string[] categoryValues, string url, string? formattedDate = null)
    {
        var adjustedSize = (long)(item.Size * sizeMultiplier);
        if (!string.IsNullOrEmpty(item.UrlSubtitle))
        {
            adjustedSize += 15000000; // Add 15MB to size if subs are available
        }
        var parsedTitle = GenerateTitle(tvdbData, item.Topic, item.Title, quality, formattedDate, season, episode);
        var formattedTitle = FormatTitle(parsedTitle);
        //var translatedTitle = TranslateTitle(formattedTitle, tvdbData);
        var translatedTitle = formattedTitle; // TODO see if translation is needed
        var encodedTitle = Convert.ToBase64String(Encoding.UTF8.GetBytes(translatedTitle));
        var encodedVideoUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));
        var encodedSubtitleUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.UrlSubtitle));

        // Generate the full URL for the fake_nzb_download endpoint
        var fakeDownloadUrl = $"/api/fake_nzb_download?encodedVideoUrl={encodedVideoUrl}&encodedSubtitleUrl={encodedSubtitleUrl}&encodedTitle={encodedTitle}";
        return new Item
        {
            Title = translatedTitle,
            Guid = new Guid
            {
                IsPermaLink = true,
                Value = $"{item.UrlWebsite}#{quality}{(string.IsNullOrEmpty(formattedDate) ? "" : "-a")}-{item.Language}",
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
            Attributes = NewznabUtils.GenerateAttributes(yearSeason ?? season, episode, categoryValues)
        };
    }

    // TODO refactor and make this look good, It's too late right now:D
    // TODO now it's even worse :D oh god
    private static string GenerateTitle(TvdbData? tvdbData, string topic, string title, string quality, string? formattedDate, string? seasonOverride, string? episodeOverride)
    {
        var showName = tvdbData?.Name ?? topic;
        var language = title.Contains("(Englisch)") ? "ENGLISH" : "GERMAN";

        if (!string.IsNullOrEmpty(formattedDate))
        {
            var cleanedTitle = EpisodeRegex().Replace(title, "").Trim();

            if (cleanedTitle == topic)
            {
                cleanedTitle = null;
            }

            return $"{showName}.{formattedDate}.{(cleanedTitle != null ? $"{cleanedTitle}." : "")}{language}.{quality}.WEB.h264.MATCH.UNCERTAIN-MEDiATHEK".Replace(" ", ".");
        }
        var episodePattern = @"S\d{1,4}/E\d{1,4}";
        var match = Regex.Match(title, episodePattern);

        if (match.Success)
        {
            var seasonAndEpisode = match.Value.Replace("/", "");
            var cleanedTitle = EpisodeRegex().Replace(title, "").Replace($"({match.Value})", "").Trim();

            if (cleanedTitle == topic)
            {
                cleanedTitle = null;
            }

            if (seasonOverride is null || episodeOverride is null)
            {
                // use data from mediathek
                return $"{topic}.{seasonAndEpisode}.{(cleanedTitle != null ? $"{cleanedTitle}." : "")}{language}.{quality}.WEB.h264.MATCH.UNCERTAIN-MEDiATHEK".Replace(" ", ".");
            }

            // use overwrite data
            var zeroBasedSeason = seasonOverride.Length >= 2 ? seasonOverride : $"0{seasonOverride}";
            var zeroBasedEpisode = episodeOverride.Length >= 2 ? episodeOverride : $"0{episodeOverride}";
            return $"{showName}.S{zeroBasedSeason}E{zeroBasedEpisode}.{(cleanedTitle != null ? $"{cleanedTitle}." : "")}{language}.{quality}.WEB.h264.MATCH.UNCERTAIN-MEDiATHEK".Replace(" ", ".");
        }

        if (seasonOverride is null || episodeOverride is null)
        {
            return $"{showName} - {title}.{language}.{quality}.WEB.h264.NO.MATCH-MEDiATHEK";
        }
        else
        {
            var cleanedTitle = EpisodeRegex().Replace(title, "").Trim();

            if (cleanedTitle == topic)
            {
                cleanedTitle = null;
            }

            var zeroBasedSeason = seasonOverride.Length >= 2 ? seasonOverride : $"0{seasonOverride}";
            var zeroBasedEpisode = episodeOverride.Length >= 2 ? episodeOverride : $"0{episodeOverride}";

            return $"{showName}.S{zeroBasedSeason}E{zeroBasedEpisode}.{(cleanedTitle != null ? $"{cleanedTitle}." : title)}GERMAN.{quality}.WEB.h264.MATCH.UNCERTAIN-MEDiATHEK".Replace(" ", ".");
        }
    }
    private static string FormatTitle(string title)
    {
        // Replace German Umlaute and special characters
        title = title.Replace("ä", "ae")
                     .Replace("ö", "oe")
                     .Replace("ü", "ue")
                     .Replace("ß", "ss")
                     .Replace("Ä", "Ae")
                     .Replace("Ö", "Oe")
                     .Replace("Ü", "Ue");

        // Remove "Englisch" at the end
        title = title.Replace(".(Englisch)", "");

        // Remove unwanted characters
        title = TitleRegexUnd().Replace(title, "und");
        title = TitleRegexSymbols().Replace(title, ""); // Remove various symbols
        title = TitleRegexWhitespace().Replace(title, ".").Replace("..", ".");

        return title;
    }

    private static MediathekApiResponse? ApplyFilters(string apiResponse, TvdbEpisode episode)
    {
        var responseObject = JsonSerializer.Deserialize<MediathekApiResponse>(apiResponse);

        if (responseObject?.Result?.Results == null)
        {
            return null;
        }

        var initialResults = responseObject.Result.Results;
        var resultsFilteredByRuntime = FilterByRuntime(initialResults, episode.Runtime);
        var resultsByTitleDate = FilterByTitleDate(resultsFilteredByRuntime, episode.Aired).Where(item => !MediathekSearchService.ShouldSkipItem(item)).ToList();
        var resultsByDescriptionDate = FilterByDescriptionDate(resultsFilteredByRuntime, episode.Aired).Where(item => !MediathekSearchService.ShouldSkipItem(item)).ToList();
        var resultsByEpisodeTitleMatch = FilterByEpisodeTitleMatch(resultsFilteredByRuntime, episode.Name).Where(item => !MediathekSearchService.ShouldSkipItem(item)).ToList();
        List<ApiResultItem> resultsBySeasonEpisodeMatch = [];
        // if more than 3 results we assume episode title match wasn't correct
        if (resultsByEpisodeTitleMatch.Count > 3)
        {
            resultsByEpisodeTitleMatch.Clear();
        }

        if (resultsByTitleDate.Count == 0 && resultsByDescriptionDate.Count == 0 && resultsByEpisodeTitleMatch.Count == 0)
        {
            // Only trust Mediathek season/episode if no other match (aside from airedDate):
            resultsBySeasonEpisodeMatch =
                FilterBySeasonEpisodeMatch(resultsFilteredByRuntime, episode.SeasonNumber.ToString(), episode.EpisodeNumber.ToString())
                .Where(item => !MediathekSearchService.ShouldSkipItem(item)).ToList(); ;
        }

        // HashSet to remove duplicates
        HashSet<ApiResultItem> filteredResults = [ .. resultsByTitleDate, .. resultsByDescriptionDate, .. resultsByEpisodeTitleMatch, .. resultsBySeasonEpisodeMatch];

        // Create a filtered API response
        var filteredApiResponse = new MediathekApiResponse
        {
            Result = new MediathekApiResult
            {
                Results = [.. filteredResults],
                QueryInfo = responseObject.Result.QueryInfo
            },
            Err = responseObject.Err
        };

        return filteredApiResponse;
    }

    private static List<ApiResultItem> FilterByRuntime(List<ApiResultItem> results, int? runtime)
    {
        if (runtime is null || runtime is 0)
        {
            return results;
        }
        var minRuntime = Math.Max(5, (int)(runtime * 0.65)) * 60;
        var maxRuntime = (int)(runtime * 1.35) * 60;
        return results.Where(item =>
            item.Duration >= minRuntime && item.Duration <= maxRuntime)
            .ToList();
    }

    private static List<ApiResultItem> FilterByAiredDate(List<ApiResultItem> results, DateTime? airedDate)
    {
        if (airedDate is null)
        {
            return [];
        }
        return results.Where(item =>
            ConvertToBerlinTimezone(UnixTimeStampToDateTime(item.Timestamp)).Date == airedDate)
            .ToList();
    }

    private static List<ApiResultItem> FilterByTitleDate(List<ApiResultItem> results, DateTime? airedDate)
    {
        if (airedDate is null)
        {
            return [];
        }

        var formattedAiredDate = airedDate.Value.ToString("yyyy-MM-dd");

        return results.Where(item =>
        {
            var extractedDate = ExtractDate(item.Title);
            return !string.IsNullOrEmpty(extractedDate) && extractedDate == formattedAiredDate;
        }).ToList();
    }

    private static List<ApiResultItem> FilterByDescriptionDate(List<ApiResultItem> results, DateTime? airedDate)
    {
        if (airedDate is null)
        {
            return [];
        }

        var formattedAiredDate = airedDate.Value.ToString("yyyy-MM-dd");

        return results.Where(item =>
        {
            var extractedDate = ExtractDate(item.Description);
            return !string.IsNullOrEmpty(extractedDate) && extractedDate == formattedAiredDate;
        }).ToList();
    }


    private static List<ApiResultItem> FilterByEpisodeTitleMatch(List<ApiResultItem> results, string episodeName)
    {
        var normalizedEpisodeName = NormalizeString(episodeName);

        return results.Where(item =>
        {
            var normalizedTitle = NormalizeString(item.Title);
            return normalizedTitle.Contains(normalizedEpisodeName, StringComparison.OrdinalIgnoreCase);
        }).ToList();
    }

    private static List<ApiResultItem> FilterBySeasonEpisodeMatch(List<ApiResultItem> results, string season, string episode)
    {
        var zeroBasedSeason = season.Length >= 2 ? season : $"0{season}";
        var zeroBasedEpisode = episode.Length >= 2 ? episode : $"0{episode}";

        return results.Where(item =>
        {
            return item.Title.Contains($"S{zeroBasedSeason}") && item.Title.Contains($"E{zeroBasedEpisode}");
        }).ToList();
    }

    // Normalize a string to remove special characters and retain only A-Z, äöüÄÖÜß
    private static string NormalizeString(string input)
    {
        var regex = NormalizeRegex();
        return regex.Replace(input, "").ToLowerInvariant();
    }

    private static string ExtractDate(string title)
    {
        // Numeric format pattern (e.g., "24.10.2024" or "24.10.24")
        var numericDatePattern = @"(\d{1,2})\.(\d{1,2})\.(\d{2}|\d{4})";
        // Nonth name format pattern (e.g., "16. Juli 2024")
        var germanMonthPattern = @"(\d{1,2})\.\s*(\w+)\s+(\d{4})";

        var numericDateMatch = Regex.Match(title, numericDatePattern);
        if (numericDateMatch.Success)
        {
            int day = int.Parse(numericDateMatch.Groups[1].Value);
            int month = int.Parse(numericDateMatch.Groups[2].Value);
            int year = int.Parse(numericDateMatch.Groups[3].Value);

            if (year < 100)
            {
                year += 2000;
            }

            DateTime date = new(year, month, day);
            return date.ToString("yyyy-MM-dd");
        }

        var longMonthMatch = Regex.Match(title, germanMonthPattern);
        if (longMonthMatch.Success)
        {
            int day = int.Parse(longMonthMatch.Groups[1].Value);
            string monthName = longMonthMatch.Groups[2].Value;
            int year = int.Parse(longMonthMatch.Groups[3].Value);

            var germanCulture = new CultureInfo("de-DE");
            if (DateTime.TryParseExact($"{day} {monthName} {year}",
                                       "d MMMM yyyy",
                                       germanCulture,
                                       DateTimeStyles.None,
                                       out DateTime date))
            {
                return date.ToString("yyyy-MM-dd");
            }
        }

        return string.Empty;
    }
    private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).UtcDateTime;
    }

    private static DateTime ConvertToBerlinTimezone(DateTime utcDateTime)
    {
        var berlinTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, berlinTimeZone);
    }

    [GeneratedRegex(@"[&]")]
    private static partial Regex TitleRegexUnd();
    [GeneratedRegex(@"[/:;""'@#?$%^*+=!<>,()]")]
    private static partial Regex TitleRegexSymbols();
    [GeneratedRegex(@"\s+")]
    private static partial Regex TitleRegexWhitespace();
    [GeneratedRegex(@"Folge\s*\d+:\s*")]
    private static partial Regex EpisodeRegex();
    [GeneratedRegex("[^a-zA-ZäöüÄÖÜß]")]
    private static partial Regex NormalizeRegex();
}

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using MediathekArr.Models;
using Microsoft.Extensions.Caching.Memory;
using Guid = MediathekArr.Models.Guid;

namespace MediathekArr.Services
{
    public partial class MediathekSearchService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        private readonly IMemoryCache _cache = cache;
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient("MediathekClient");
        private readonly TimeSpan _cacheTimeSpan = TimeSpan.FromMinutes(55);
        private static readonly string[] SkipKeywords = ["(Audiodeskription)", "(klare Sprache)", "(Gebärdensprache)", "Trailer", "Outtakes:"];
        private static readonly string[] queryField = ["topic"];
        public async Task<string> FetchSearchResultsFromApiById(TvdbData tvdbData, string? season, string? episodeNumber)
        {
            var cacheKey = $"tvdb_{tvdbData.Id}_{season ?? "null"}_{episodeNumber ?? "null"}";

            if (_cache.TryGetValue(cacheKey, out string? cachedResponse))
            {
                return cachedResponse ?? "";
            }

            // Find correct episode in tvdbData
            TvdbEpisode? episode;
            if (season?.Length == 4 && (episodeNumber?.Contains('/') ?? false))
            {
                var episodeNumberSplitted = episodeNumber?.Split('/');
                if (episodeNumberSplitted?.Length == 2 && DateTime.TryParse($"{season}-{episodeNumberSplitted[0]}-{episodeNumberSplitted[1]}", out DateTime searchAirDate))
                {
                    episode = tvdbData.FindEpisodeByAirDate(searchAirDate);
                }
                else
                {
                    episode = null;
                }
            }
            else
            {
                episode = tvdbData.FindEpisodeBySeasonAndNumber(season, episodeNumber);
            }
            if (episode is null || episode.Aired is null || episode.Aired.Value.Year <= 1970)
            {
                _cache.Set(cacheKey, string.Empty, _cacheTimeSpan);
                return ConvertIdSearchApiResponseToRss(null, string.Empty, string.Empty, tvdbData);
            }

            var queries = new List<object>();
            var searchName = string.IsNullOrEmpty(tvdbData.GermanName) ? tvdbData.Name : tvdbData.GermanName;
            queries.Add(new { fields = queryField, query = searchName });

            var requestBody = new
            {
                queries,
                sortBy = "timestamp",
                sortOrder = "desc",
                future = false,
                offset = 0,
                size = 5000 // 5000 for id search
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8);

            var response = await _httpClient.PostAsync("https://mediathekviewweb.de/api/query", requestContent);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStringAsync();
                var filteredResponse = ApplyFilters(apiResponse, episode);

                var newznabRssResponse = ConvertIdSearchApiResponseToRss(filteredResponse, episode.SeasonNumber.ToString(), episode.EpisodeNumber.ToString(), tvdbData);
                _cache.Set(cacheKey, newznabRssResponse, _cacheTimeSpan);

                return newznabRssResponse;
            }

            return null;
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
            var resultsByAiredDate = FilterByAiredDate(resultsFilteredByRuntime, episode.Aired!.Value).Where(item => !ShouldSkipItem(item)).ToList();
            var resultsByTitleDate = FilterByTitleDate(resultsFilteredByRuntime, episode.Aired.Value).Where(item => !ShouldSkipItem(item)).ToList();
            var resultsByDescriptionDate = FilterByDescriptionDate(resultsFilteredByRuntime, episode.Aired.Value).Where(item => !ShouldSkipItem(item)).ToList();
            var resultsByEpisodeTitleMatch = FilterByEpisodeTitleMatch(resultsFilteredByRuntime, episode.Name).Where(item => !ShouldSkipItem(item)).ToList();
            List<ApiResultItem> resultsBySeasonEpisodeMatch = [];
            // if more than 3 results we assume episode title match wasn't correct
            if (resultsByEpisodeTitleMatch.Count > 3)
            {
                resultsByEpisodeTitleMatch.Clear();
            }

            // if we have episode title match that is the best we got
            if (resultsByEpisodeTitleMatch.Count > 0)
            {
                // we ignore air date in this case as it is not as reliable
                resultsByAiredDate.Clear();
            }

            if (resultsByAiredDate.Count == 0 && resultsByTitleDate.Count == 0 && resultsByDescriptionDate.Count == 0 && resultsByEpisodeTitleMatch.Count == 0)
            {
                // Only trust Mediathek season/episode if no other match:
                resultsBySeasonEpisodeMatch =
                    FilterBySeasonEpisodeMatch(resultsFilteredByRuntime, episode.SeasonNumber.ToString(), episode.EpisodeNumber.ToString())
                    .Where(item => !ShouldSkipItem(item)).ToList(); ;
            }

            // HashSet to remove duplicates
            HashSet<ApiResultItem> filteredResults = [.. resultsByAiredDate, .. resultsByTitleDate, .. resultsByDescriptionDate, .. resultsByEpisodeTitleMatch, .. resultsBySeasonEpisodeMatch];

            // Create a filtered API response
            var filteredApiResponse = new MediathekApiResponse
            {
                Result = new Result
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

        private static List<ApiResultItem> FilterByAiredDate(List<ApiResultItem> results, DateTime airedDate)
        {
            return results.Where(item =>
                ConvertToBerlinTimezone(UnixTimeStampToDateTime(item.Timestamp)).Date == airedDate)
                .ToList();
        }

        private static List<ApiResultItem> FilterByTitleDate(List<ApiResultItem> results, DateTime airedDate)
        {
            var formattedAiredDate = airedDate.ToString("yyyy-MM-dd");

            return results.Where(item =>
            {
                var extractedDate = ExtractDate(item.Title);
                return !string.IsNullOrEmpty(extractedDate) && extractedDate == formattedAiredDate;
            }).ToList();
        }

        private static List<ApiResultItem> FilterByDescriptionDate(List<ApiResultItem> results, DateTime airedDate)
        {
            var formattedAiredDate = airedDate.ToString("yyyy-MM-dd");

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
                if (normalizedTitle.Contains(normalizedEpisodeName, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
                else if (normalizedEpisodeName.Length >= 13 && normalizedTitle.Length >= 10)
                {
                    return normalizedEpisodeName.Contains(normalizedTitle, StringComparison.OrdinalIgnoreCase);
				}
                else
                {
                    return false;
                }
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

        public async Task<string> FetchSearchResultsFromApiByString(string? q, string? season)
        {
            var cacheKey = $"q_{q ?? "null"}_{season ?? "null"}";

            if (_cache.TryGetValue(cacheKey, out string? cachedResponse))
            {
                return cachedResponse ?? "";
            }

            var zeroBasedSeason = season == null || season.Length >= 2 ? season : $"0{season}";
            
            var queries = new List<object>();
            if (q != null)
            {
                queries.Add(new { fields = queryField, query = q });
            }

            if (!string.IsNullOrEmpty(season))
            {
                if (season.Length == 4 && season.StartsWith("20") || season.StartsWith("19"))
                {
                    queries.Add(new { fields = new[] { "title" }, query = $"{season}" });
                }
                else
                {
                    queries.Add(new { fields = new[] { "title" }, query = $"S{zeroBasedSeason}" });
                }
            }

            var requestBody = new
            {
                queries,
                sortBy = "timestamp",
                sortOrder = "desc",
                future = false,
                offset = 0,
                size = 300 // 300 for RSS sync and string search
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8);

            var response = await _httpClient.PostAsync("https://mediathekviewweb.de/api/query", requestContent);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStringAsync();
                var newznabRssResponse = ConvertStringSearchApiResponseToRss(apiResponse, season);
                _cache.Set(cacheKey, newznabRssResponse, _cacheTimeSpan);

                return newznabRssResponse;
            }

            return null;
        }

        private string ConvertIdSearchApiResponseToRss(MediathekApiResponse? filteredResponse, string season, string episode, TvdbData tvdbData)
        {
            if (filteredResponse is null || filteredResponse.Result.Results == null)
            {
                return SerializeRss(GetEmptyRssResult());
            }

            var rss = new Rss
            {
                Channel = new Channel
                {
                    Title = "MediathekArr",
                    Description = "MediathekArr API results",
                    Response = new NewznabResponse
                    {
                        Offset = 0,
                        Total = filteredResponse.Result.QueryInfo.ResultCount
                    },
                    Items = filteredResponse.Result.Results
                        // .Where(item => !ShouldSkipItem(item)) we already do this earlier for id searches in ApplyFilters
                        .SelectMany(item => GenerateRssItems(item, season, episode, tvdbData)) // Generate RSS items for each link
                        .ToList()
                }
            };

            return SerializeRss(rss);
        }

        private string ConvertStringSearchApiResponseToRss(string apiResponse, string? season = null, bool sonarr = true)
        {
            if (string.IsNullOrWhiteSpace(apiResponse))
            {
                return SerializeRss(GetEmptyRssResult());
            }

            var responseObject = JsonSerializer.Deserialize<MediathekApiResponse>(apiResponse);

            if (responseObject?.Result?.Results == null)
            {
                return SerializeRss(GetEmptyRssResult());
            }

            var rss = new Rss
            {
                Channel = new Channel
                {
                    Title = "MediathekArr",
                    Description = "MediathekArr API results",
                    Response = new NewznabResponse
                    {
                        Offset = 0,
                        Total = responseObject.Result.QueryInfo.ResultCount
                    },
                    Items = responseObject.Result.Results
                        .Where(item => !ShouldSkipItem(item))
                        .SelectMany(item => GenerateRssItems(item, season, null)) // Generate RSS items for each link
                        .ToList()
                }
            };

            return SerializeRss(rss);
        }

        private Rss GetEmptyRssResult()
        {
            return new Rss
            {
                Channel = new Channel
                {
                    Title = "MediathekArr",
                    Description = "MediathekArr API results",
                    Response = new NewznabResponse
                    {
                        Offset = 0,
                        Total = 0
                    },
                    Items = []
                }
            };
        }

        private List<Item> GenerateRssItems(ApiResultItem item, string? season, string? episode, TvdbData? tvdbData = null)
        {
            var items = new List<Item>();

            string[] categories = ["5000", "2000"];

            if (!string.IsNullOrEmpty(item.UrlVideoHd))
            {
                items.AddRange(CreateRssItems(item, season, episode, tvdbData, "1080p", 1.6, "TV > HD", [..categories, "5040", "2040"], item.UrlVideoHd));
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

        private List<Item> CreateRssItems(ApiResultItem item, string? season, string? episode, TvdbData? tvdbData, string quality, double sizeMultiplier, string category, string[] categoryValues, string url)
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

            // Remove unwanted characters
            title = TitleRegexUnd().Replace(title, "und");
            title = TitleRegexSymbols().Replace(title, ""); // Remove various symbols
            title = TitleRegexWhitespace().Replace(title, ".").Replace("..", ".");

            return title;
        }


        private Item CreateRssItem(ApiResultItem item, string? yearSeason, string? season, string? episode, TvdbData? tvdbData, string quality, double sizeMultiplier, string category, string[] categoryValues, string url, string? formattedDate = null)
        {
            var adjustedSize = (long)(item.Size * sizeMultiplier);
            var parsedTitle = GenerateTitle(item.Topic, item.Title, quality, formattedDate, season, episode);
            var formattedTitle = FormatTitle(parsedTitle);
            //var translatedTitle = TranslateTitle(formattedTitle, tvdbData);
            var translatedTitle = formattedTitle; // TODO see if translation is needed
            var encodedTitle = Convert.ToBase64String(Encoding.UTF8.GetBytes(translatedTitle));
            var encodedUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));

            // Generate the full URL for the fake_nzb_download endpoint
            var fakeDownloadUrl = $"/api/fake_nzb_download?encodedUrl={encodedUrl}&encodedTitle={encodedTitle}";

            return new Item
            {
                Title = translatedTitle,
                Guid = new Guid
                {
                    IsPermaLink = true,
                    Value = $"{item.UrlWebsite}#{quality}{(string.IsNullOrEmpty(formattedDate) ? "" : "-a")}",
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
                Attributes = GenerateAttributes(yearSeason ?? season, categoryValues)
            };
        }

        private static string TranslateTitle(string title, TvdbData? tvdbData)
        {
            if (tvdbData is null)
            {
                return title;
            }

            return title.Replace(tvdbData.GermanName, tvdbData.Name, StringComparison.OrdinalIgnoreCase);
        }

        // TODO refactor and make this look good, It's too late right now:D
        // TODO now it's even worse :D oh god
        private string GenerateTitle(string topic, string title, string quality, string? formattedDate, string? seasonOverride, string? episodeOverride)
        {
            if (!string.IsNullOrEmpty(formattedDate))
            {
                var cleanedTitle = EpisodeRegex().Replace(title, "").Trim();

                if (cleanedTitle == topic)
                {
                    cleanedTitle = null;
                }

                return $"{topic}.{formattedDate}.{(cleanedTitle != null ? $"{cleanedTitle}." : "")}GERMAN.{quality}.WEB.h264-MEDiATHEK".Replace(" ", ".");
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
                    return $"{topic}.{seasonAndEpisode}.{(cleanedTitle != null ? $"{cleanedTitle}." : "")}GERMAN.{quality}.WEB.h264-MEDiATHEK".Replace(" ", ".");
                }

                // use overwrite data
                var zeroBasedSeason = seasonOverride.Length >= 2 ? seasonOverride : $"0{seasonOverride}";
                var zeroBasedEpisode = episodeOverride.Length >= 2 ? episodeOverride : $"0{episodeOverride}";
                return $"{topic}.S{zeroBasedSeason}E{zeroBasedEpisode}.{(cleanedTitle != null ? $"{cleanedTitle}." : "")}GERMAN.{quality}.WEB.h264-MEDiATHEK".Replace(" ", ".");
            }

            if (seasonOverride is null || episodeOverride is null)
            {
                return title;
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

                return $"{topic}.S{zeroBasedSeason}E{zeroBasedEpisode}.{(cleanedTitle != null ? $"{cleanedTitle}." : title)}GERMAN.{quality}.WEB.h264-MEDiATHEK".Replace(" ", ".");
            }
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

                DateTime date = new DateTime(year, month, day);
                return date.ToString("yyyy-MM-dd");
            }

            var longMonthMatch = Regex.Match(title, germanMonthPattern);
            if (longMonthMatch.Success)
            {
                int day = int.Parse(longMonthMatch.Groups[1].Value);
                string monthName = longMonthMatch.Groups[2].Value;
                int year = int.Parse(longMonthMatch.Groups[3].Value);

                var germanCulture = new CultureInfo("de-DE");
                DateTime date;
                if (DateTime.TryParseExact($"{day} {monthName} {year}",
                                           "d MMMM yyyy",
                                           germanCulture,
                                           DateTimeStyles.None,
                                           out date))
                {
                    return date.ToString("yyyy-MM-dd");
                }
            }

            return string.Empty;
        }

        private List<NewznabAttribute> GenerateAttributes(string? season, string[] categoryValues)
        {
            var attributes = new List<NewznabAttribute>();

            foreach (var categoryValue in categoryValues)
            {
                attributes.Add(new NewznabAttribute { Name = "category", Value = categoryValue });
            }

            if (season != null)
            {
                attributes.Add(new NewznabAttribute { Name = "season", Value = season });
            }

            return attributes;
        }

        private static bool ShouldSkipItem(ApiResultItem item)
        {
            return item.UrlVideo.EndsWith(".m3u8") || SkipKeywords.Any(item.Title.Contains);
        }

        private string SerializeRss(Rss rss)
        {
            var serializer = new XmlSerializer(typeof(Rss));

            // Define the namespaces and add the newznab namespace
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("newznab", "http://www.newznab.com/DTD/2010/feeds/attributes/");

            using var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, rss, namespaces);

            // TODO quick fix
            string result = stringWriter.ToString();
            result = result.Replace(":newznab_x003A_", ":");

            return result;
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
}
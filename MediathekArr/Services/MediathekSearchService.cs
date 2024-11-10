using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using MediathekArr.Models;
using MediathekArr.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Guid = MediathekArr.Models.Guid;

namespace MediathekArr.Services
{
    public partial class MediathekSearchService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        private readonly IMemoryCache _cache = cache;
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient("MediathekClient");
        private static readonly string[] SkipKeywords = ["(Audiodeskription)", "(klare Sprache)", "(Gebärdensprache)"];
        private static readonly string[] queryField = ["topic"];

        public async Task<string> FetchSearchResultsFromApi(string? q, string? season)
        {
            // TODO attention, update cacheKey if arguments of this method change
            var cacheKey = $"{q ?? "null"}_{season ?? "null"}";

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
                size = 300
            };

            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8);

            var response = await _httpClient.PostAsync("https://mediathekviewweb.de/api/query", requestContent);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadAsStringAsync();
                var newznabRssResponse = ConvertApiResponseToRss(apiResponse, season);
                _cache.Set(cacheKey, newznabRssResponse, TimeSpan.FromMinutes(10));

                return newznabRssResponse;
            }

            return null;
        }

        private string ConvertApiResponseToRss(string apiResponse, string? season = null, bool sonarr = true)
        {
            var responseObject = JsonSerializer.Deserialize<MediathekApiResponse>(apiResponse);

            if (responseObject?.Result?.Results == null)
            {
                return string.Empty;
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
                        .Where(item => !ShouldSkipItem(item.Title))
                        .SelectMany(item => GenerateRssItems(item, season)) // Generate RSS items for each link
                        .ToList()
                }
            };

            return SerializeRss(rss);
        }

        private List<Item> GenerateRssItems(ApiResultItem item, string? season)
        {
            var items = new List<Item>();

            string[] categories = ["5000", "2000"];

            if (!string.IsNullOrEmpty(item.UrlVideoHd))
            {
                items.AddRange(CreateRssItems(item, season, "1080p", 1.6, "TV > HD", [..categories, "5040", "2040"], item.UrlVideoHd));
            }

            if (!string.IsNullOrEmpty(item.UrlVideo))
            {
                items.AddRange(CreateRssItems(item, season, "720p", 1.0, "TV > HD", [.. categories, "5040", "2040"], item.UrlVideo));
            }

            if (!string.IsNullOrEmpty(item.UrlVideoLow))
            {
                items.AddRange(CreateRssItems(item, season, "480p", 0.4, "TV > SD", [.. categories, "5030", "2030"], item.UrlVideoLow));

            }

            return items;
        }

        private List<Item> CreateRssItems(ApiResultItem item, string? season, string quality, double sizeMultiplier, string category, string[] categoryValues, string url)
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
                    items.Add(CreateRssItem(item, formattedDate, quality, sizeMultiplier, category, categoryValues, url, formattedDate));
                }
            }

         items.Add(CreateRssItem(item, season, quality, sizeMultiplier, category, categoryValues, url));

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


        private Item CreateRssItem(ApiResultItem item, string? season, string quality, double sizeMultiplier, string category, string[] categoryValues, string url, string? formattedDate = null)
        {
            var adjustedSize = (long)(item.Size * sizeMultiplier);
            var parsedTitle = GenerateTitle(item.Topic, item.Title, quality, formattedDate);
            var formattedTitle = FormatTitle(parsedTitle);
            var encodedTitle = Convert.ToBase64String(Encoding.UTF8.GetBytes(formattedTitle));
            var encodedUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(url));

            // Generate the full URL for the fake_nzb_download endpoint
            var fakeDownloadUrl = $"/api/fake_nzb_download?encodedUrl={encodedUrl}&encodedTitle={encodedTitle}";

            return new Item
            {
                Title = formattedTitle,
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
                Attributes = GenerateAttributes(season, categoryValues)
            };
        }

        // TODO refactor and make this look good, It's too late right now:D
        private string GenerateTitle(string topic, string title, string quality, string? formattedDate)
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
                var season = match.Value.Replace("/", "");
                var cleanedTitle = EpisodeRegex().Replace(title, "").Replace($"({match.Value})", "").Trim();

                if (cleanedTitle == topic)
                {
                    cleanedTitle = null;
                }

                return $"{topic}.{season}.{(cleanedTitle != null ? $"{cleanedTitle}." : "")}GERMAN.{quality}.WEB.h264-MEDiATHEK".Replace(" ", ".");
            }

            return title;
        }

        private string ExtractDate(string title)
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
                DateTime date = DateTime.ParseExact($"{day} {monthName} {year}", "d MMMM yyyy", germanCulture);

                return date.ToString("yyyy-MM-dd");
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

        private static bool ShouldSkipItem(string title)
        {
            return SkipKeywords.Any(title.Contains);
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


        [GeneratedRegex(@"[&]")]
        private static partial Regex TitleRegexUnd();
        [GeneratedRegex(@"[/:;""'@#?$%^*+=!<>]")]
        private static partial Regex TitleRegexSymbols();
        [GeneratedRegex(@"\s+")]
        private static partial Regex TitleRegexWhitespace();
        [GeneratedRegex(@"Folge\s*\d+:\s*")]
        private static partial Regex EpisodeRegex();
    }
}
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using MediathekArr.Models;
using MediathekArr.Utilities;
using Guid = MediathekArr.Models.Guid;

namespace MediathekArr.Services
{
    public partial class MediathekSearchService(IHttpClientFactory httpClientFactory)
    {
        private readonly HttpClient _httpClient = httpClientFactory.CreateClient("MediathekClient");
        private static readonly string[] SkipKeywords = ["(Audiodeskription)", "(klare Sprache)", "(Gebärdensprache)"];

        public async Task<string> FetchSearchResultsFromApi(string? q, string? season)
        {
            var zeroBasedSeason = season == null || season.Length >= 10 ? season : $"0{season}";

            var queries = new List<object>();
            if (q != null)
            {
                queries.Add(new { fields = new[] { "topic" }, query = q });
            }

            if (!string.IsNullOrEmpty(season))
            {
                queries.Add(new { fields = new[] { "title" }, query = $"S{zeroBasedSeason}" });
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
                return ConvertApiResponseToRss(apiResponse, season);
            }

            return null;
        }

        private string ConvertApiResponseToRss(string apiResponse, string? season = null)
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

            string xmlWithFixedSabnzbdNamespace = XmlHelper.SerializeToXmlWithSabnzbdNamespace(rss);

            // dirty quick fix TODO


            return xmlWithFixedSabnzbdNamespace;
        }

        private List<Item> GenerateRssItems(ApiResultItem item, string? season)
        {
            var items = new List<Item>();

            if (!string.IsNullOrEmpty(item.UrlVideoHd))
            {
                items.Add(CreateRssItem(item, season, "1080p", 1.6, "TV > HD", "5040", item.UrlVideoHd));
            }

            if (!string.IsNullOrEmpty(item.UrlVideo))
            {
                items.Add(CreateRssItem(item, season, "720p", 1.0, "TV > HD", "5040", item.UrlVideo));
            }

            if (!string.IsNullOrEmpty(item.UrlVideoLow))
            {
                items.Add(CreateRssItem(item, season, "480p", 0.4, "TV > SD", "5030", item.UrlVideoLow));
            }

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
            title = TitleRegexWhitespace().Replace(title, ".");

            return title;
        }


        private Item CreateRssItem(ApiResultItem item, string? season, string quality, double sizeMultiplier, string category, string categoryValue, string url)
        {
            var adjustedSize = (long)(item.Size * sizeMultiplier);
            var parsedTitle = ParseTitle(item.Topic, item.Title, quality);
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
                    Value = $"{item.UrlWebsite}#{quality}",
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
                Attributes = GenerateAttributes(season, categoryValue)
            };
        }

        private string ParseTitle(string topic, string title, string quality)
        {
            var episodePattern = @"S\d{1,4}/E\d{1,4}";
            var match = Regex.Match(title, episodePattern);

            if (match.Success)
            {
                var cleanedTitle = EpisodeRegex().Replace(title, "").Replace($"({match.Value})", "").Trim();

                if (cleanedTitle == topic)
                {
                    cleanedTitle = null;
                }

                return $"{topic}.{match.Value.Replace("/", "")}{(cleanedTitle != null ? $".{cleanedTitle}." : ".")}GERMAN.{quality}.WEB.x264-MEDiATHEK".Replace(" ", ".");
            }

            return title;
        }

        private List<NewznabAttribute> GenerateAttributes(string? season, string categoryValue)
        {
            var attributes = new List<NewznabAttribute>
            {
                new NewznabAttribute { Name = "category", Value = categoryValue }
            };

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
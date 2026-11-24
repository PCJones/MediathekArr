using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace MediathekArr.Services;

/// <summary>
/// Resolves an IMDb id to the titles a movie may carry in the German Mediatheken
/// (German, original and English title) plus its release year.
/// Sources: IMDb suggestion endpoint (title + year) and Wikidata (German/original label).
/// Both are public and need no API key.
/// </summary>
public class MovieLookupService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache, ILogger<MovieLookupService> logger)
{
    private readonly HttpClient _httpClient = CreateClient(httpClientFactory);
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<MovieLookupService> _logger = logger;

    public record MovieInfo(string ImdbId, List<string> Titles, int? Year);

    private static HttpClient CreateClient(IHttpClientFactory factory)
    {
        var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MediathekArr/1.0 (movie lookup)");
        return client;
    }

    /// <summary>Accepts "tt0123456", "0123456" or "123456"; returns "tt" + at least 7 digits, or null.</summary>
    public static string? NormalizeImdbId(string? imdbid)
    {
        if (string.IsNullOrWhiteSpace(imdbid))
        {
            return null;
        }

        var digits = imdbid.Trim().ToLowerInvariant().Replace("tt", "");
        if (digits.Length == 0 || !digits.All(char.IsDigit))
        {
            return null;
        }

        return "tt" + digits.TrimStart('0').PadLeft(7, '0');
    }

    public async Task<MovieInfo?> GetMovieInfoByImdbId(string? rawImdbId)
    {
        var imdbId = NormalizeImdbId(rawImdbId);
        if (imdbId is null)
        {
            return null;
        }

        var cacheKey = $"MovieInfo_{imdbId}";
        if (_memoryCache.TryGetValue(cacheKey, out MovieInfo? cached))
        {
            return cached;
        }

        var titles = new List<string>();
        int? year = null;

        // Wikidata first: German title should be the preferred (first) title
        try
        {
            var (wdTitles, wdYear) = await LookupWikidata(imdbId);
            titles.AddRange(wdTitles);
            year = wdYear;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Wikidata lookup for {ImdbId} failed: {Message}", imdbId, ex.Message);
        }

        try
        {
            var (imdbTitle, imdbYear) = await LookupImdbSuggestion(imdbId);
            if (!string.IsNullOrWhiteSpace(imdbTitle))
            {
                titles.Add(imdbTitle);
            }
            year = imdbYear ?? year;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("IMDb lookup for {ImdbId} failed: {Message}", imdbId, ex.Message);
        }

        titles = titles
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .DistinctBy(t => t.ToLowerInvariant())
            .ToList();

        MovieInfo? info = titles.Count == 0 ? null : new MovieInfo(imdbId, titles, year);
        _logger.LogInformation("Movie lookup {ImdbId}: {Titles} ({Year})", imdbId, string.Join(" / ", titles), year);

        _memoryCache.Set(cacheKey, info, info is null ? TimeSpan.FromHours(1) : TimeSpan.FromHours(24));
        return info;
    }

    private async Task<(string? title, int? year)> LookupImdbSuggestion(string imdbId)
    {
        var json = await _httpClient.GetStringAsync($"https://v3.sg.media-imdb.com/suggestion/x/{imdbId}.json");
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("d", out var d))
        {
            return (null, null);
        }

        foreach (var entry in d.EnumerateArray())
        {
            if (entry.TryGetProperty("id", out var id) && id.GetString() == imdbId)
            {
                var title = entry.TryGetProperty("l", out var l) ? l.GetString() : null;
                int? year = entry.TryGetProperty("y", out var y) && y.ValueKind == JsonValueKind.Number ? y.GetInt32() : null;
                return (title, year);
            }
        }

        return (null, null);
    }

    private async Task<(List<string> titles, int? year)> LookupWikidata(string imdbId)
    {
        var query = $@"SELECT ?de ?orig ?en ?d WHERE {{
  ?item wdt:P345 ""{imdbId}"".
  OPTIONAL {{ ?item rdfs:label ?de FILTER(LANG(?de) = ""de"") }}
  OPTIONAL {{ ?item wdt:P1476 ?orig }}
  OPTIONAL {{ ?item rdfs:label ?en FILTER(LANG(?en) = ""en"") }}
  OPTIONAL {{ ?item wdt:P577 ?d }}
}} LIMIT 50";

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://query.wikidata.org/sparql?query=" + Uri.EscapeDataString(query));
        request.Headers.Accept.ParseAdd("application/sparql-results+json");
        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var titles = new List<string>();
        int? year = null;

        foreach (var binding in doc.RootElement.GetProperty("results").GetProperty("bindings").EnumerateArray())
        {
            foreach (var key in new[] { "de", "orig", "en" })
            {
                if (binding.TryGetProperty(key, out var v) && v.TryGetProperty("value", out var value))
                {
                    var s = value.GetString();
                    if (!string.IsNullOrWhiteSpace(s) && !titles.Contains(s))
                    {
                        titles.Add(s);
                    }
                }
            }

            if (binding.TryGetProperty("d", out var d) && DateTime.TryParse(d.GetProperty("value").GetString(), out var date))
            {
                year = year is null ? date.Year : Math.Min(year.Value, date.Year);
            }
        }

        // keep German first, then original, then English
        return (titles, year);
    }
}

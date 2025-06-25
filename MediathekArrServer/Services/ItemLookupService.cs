using MediathekArrLib.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace MediathekArrServer.Services;

public class ItemLookupService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IMemoryCache memoryCache)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly string _apiBaseUrl = configuration["MEDIATHEKARR_API_BASE_URL"] ?? "https://mediathekarr.pcjones.de/api/v1";
    private readonly IMemoryCache _memoryCache = memoryCache;

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<TvdbData?> GetShowInfoByTvdbId(int? tvdbid)
    {
        if (tvdbid == null)
        {
            return null;
        }

        var cacheKey = $"TvdbInfo_{tvdbid}";
        if (_memoryCache.TryGetValue(cacheKey, out TvdbData? cachedTvdbInfo))
        {
            if (cachedTvdbInfo != null)
            {
                return cachedTvdbInfo;
            }
        }

        var requestUrl = $"{_apiBaseUrl}/get_show.php?tvdbid={tvdbid}";

        var response = await _httpClient.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Error fetching data: {errorContent}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tvdbInfo = JsonSerializer.Deserialize<TvdbInfoResponse>(jsonResponse, GetJsonSerializerOptions());

        if (tvdbInfo?.Status == "error")
        {
            // TODO log error message
            return null;
        }

        if (tvdbInfo == null || tvdbInfo.Status != "success" || tvdbInfo.Data == null)
        {
            throw new HttpRequestException($"Failed to fetch TVDB data. Response: {jsonResponse}");
        }

        _memoryCache.Set(cacheKey, tvdbInfo.Data, TimeSpan.FromHours(12));

        return tvdbInfo.Data;
    }
}

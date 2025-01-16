using MediathekArr.Models.Tvdb;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace MediathekArr.Services;

public class ItemLookupService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IMemoryCache memoryCache)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly string _apiBaseUrl = configuration[Constants.EnvironmentVariableConstants.Api_Base_Url] ?? Constants.MediathekArrConstants.MediathekArr_Api_Base_Url;
    private readonly IMemoryCache _memoryCache = memoryCache;

    private static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<Data?> GetShowInfoByTvdbId(int? tvdbid)
    {

        if (tvdbid == null)
        {
            return null;
        }

        var cacheKey = $"TvdbInfo_{tvdbid}";
        if (_memoryCache.TryGetValue(cacheKey, out Data? cachedTvdbInfo))
        {
            if (cachedTvdbInfo != null)
            {
                return cachedTvdbInfo;
            }
        }

        var requestUrl = $"{_apiBaseUrl}/Series?tvdbid={tvdbid}";
        if (_apiBaseUrl == "https://mediathekarr.pcjones.de/api/v1") requestUrl = $"{_apiBaseUrl}/get_show.php?tvdbid={tvdbid}";

        var response = await _httpClient.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Error fetching data: {errorContent}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var tvdbInfo = JsonSerializer.Deserialize<InfoResponse>(jsonResponse, GetJsonSerializerOptions());

        if (tvdbInfo == null || tvdbInfo.Status != "success" || tvdbInfo.Data == null)
        {
            throw new HttpRequestException($"Failed to fetch TVDB data. Response: {jsonResponse}");
            // TODO log and return null
        }

        _memoryCache.Set(cacheKey, tvdbInfo.Data, TimeSpan.FromHours(12));

        return tvdbInfo.Data;
    }
}

using MediathekArr.Clients;
using MediathekArr.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace MediathekArr.Services;

public class ItemLookupService(IMemoryCache memoryCache, ISeriesClient seriesClient, ILogger<ItemLookupService> logger)
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ISeriesClient seriesClient = seriesClient;
    private readonly ILogger<ItemLookupService> logger = logger;

    public async Task<Series?> GetShowInfoByTvdbId(int? tvdbid)
    {
        if (tvdbid == null) return null;

        /* Return cached value if possible */
        var cacheKey = $"Series_{tvdbid}";
        if (_memoryCache.TryGetValue(cacheKey, out Series? cachedTvdbInfo) && cachedTvdbInfo is not null) return cachedTvdbInfo;

        /* Fetch from API */
        var result = await seriesClient.SeriesAsync(tvdbid);
        if(result.StatusCode != (int) System.Net.HttpStatusCode.OK)
        {
            logger.LogError("Failed to fetch TVDB data. Response: {StatusCode}", result.StatusCode);
            throw new HttpRequestException($"Failed to fetch TVDB data. Response: {result.StatusCode}");
        }

        _memoryCache.Set(cacheKey, result.Result, TimeSpan.FromHours(12));

        return result.Result;
    }
}

using MediathekArr.Clients;
using MediathekArr.Infrastructure;
using Microsoft.Extensions.Caching.Memory;

namespace MediathekArr.Services;

public class ItemLookupService(ILogger<ItemLookupService> logger, IMemoryCache memoryCache, ISeriesClient seriesClient)
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ISeriesClient seriesClient = seriesClient;
    private readonly ILogger<ItemLookupService> logger = logger;

    /// <summary>
    /// Get a TV Show by its TVDB ID
    /// </summary>
    /// <param name="tvdbid"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<Series?> GetShowInfoById(int? tvdbid)
    {
        if (tvdbid == null) return null;

        /* Return cached value if possible */
        var cacheKey = $"Series_{tvdbid}";
        if (_memoryCache.TryGetValue(cacheKey, out Series? cachedResult) && cachedResult is not null) return cachedResult;

        /* Fetch from API */
        var result = await seriesClient.SeriesAsync(tvdbid);
        if (result.StatusCode != (int)System.Net.HttpStatusCode.OK)
        {
            logger.LogError("Failed to fetch TVDB data. Response: {StatusCode}", result.StatusCode);

            /* Throw HttpRequest Exception for all the consuming services to be able to return a proper http response */
            // TODO: We're doing flow control with exceptions here ... not ideal. Instead we should throw a custom exception and handle it in the controller
            throw new HttpRequestException($"Failed to fetch TVDB data. Response: {result.StatusCode}");
        }

        _memoryCache.Set(cacheKey, result.Result, TimeSpan.FromHours(Constants.MediathekArrConstants.MediathekArr_MemoryCache_Expiry));

        return result.Result;
    }
}

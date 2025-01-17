using MediathekArr.Infrastructure;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tvdb.Clients;
using Tvdb.Types;
using MediathekArr.Extensions;
using System.Net;
using Tvdb.Extensions;
using Microsoft.Extensions.Caching.Memory;
using MediathekArr.Models.Tvdb;

namespace MediathekArr.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeriesController(ILogger<SeriesController> logger, MediathekArrContext context, ISeriesClient seriesClient, IMemoryCache memoryCache) : Controller
{
    #region Properties
    public MediathekArrContext Context { get; } = context;
    public ISeriesClient SeriesClient { get; } = seriesClient;
    public IMemoryCache MemoryCache { get; } = memoryCache;
    public ILogger<SeriesController> Logger { get; } = logger;
    #endregion

    [HttpGet]
    public async Task<ActionResult<Series>> GetSeriesData([FromQuery] int tvdbId)
    {
        /* Fetch Series Data:
         * 1. Look in Memory Cache
         * 2. Look in DB Cache
         * 3. Fetch live value via TVDB API
         */

        /* Return cached Value */
        var cacheKey = $"Series_{tvdbId}";
        if(MemoryCache.TryGetValue(cacheKey, out Series? cachedResult) && cachedResult is not null)
        {
            Logger.LogTrace("Found {tvdbId} in Memory Cache", tvdbId);
            return Ok(cachedResult);
        }

        /* Return cached version of Series whenever possible */
        if (await Context.Series.AnyAsync(s => s.SeriesId == tvdbId))
        {
            Logger.LogTrace("Found {tvdbId} in Database Cache", tvdbId);
            var seriesData = await Context.Series
                .Include(s => s.Episodes)
                .FirstAsync(s => s.SeriesId == tvdbId);

            if (!seriesData.CacheExpiry.IsInThePast()) return Ok(seriesData);
            Logger.LogWarning("Series {tvdbId} in Cache has expired on {expiryDate}", tvdbId, seriesData.CacheExpiry);
        }

        /* Fetch Series from TVDB API */
        var newSeriesData = await FetchAndCacheSeriesData(tvdbId);
        if (newSeriesData == null)
        {
            Logger.LogError("Tried fetching Series {tvdbId} from TVDB and failed.", tvdbId);
            return Problem(statusCode: (int)HttpStatusCode.InternalServerError, title: "Failed to fetch data from TVDB.", detail: $"Tried fetching Series {tvdbId} from TVDB and failed.");
        }

        return Ok(newSeriesData);
    }

    private async Task<Series> FetchAndCacheSeriesData(int tvdbId)
    {
        /* Fetch from TVDB */
        var seriesData = await SeriesClient.ExtendedAsync(tvdbId, SeriesMeta.Episodes, true);
        if (!seriesData.IsSuccess) return null;

        /* Caching:
         * Place the record in our DB
         * TODO: Place a copy in the actual cache as well. Since this runs in a docker container, storing this in memory might actually boost performance :-)
         * TODO: Look into redis as cache instead of sqlite
         */
        var series = seriesData.Data;
        var germanName = series.NameTranslations.Any(t => t == "deu") ? series.NameTranslations.First(t => t == "deu") : series.Name;
        var germanAliases = series.Aliases.ToList()?.Where(a => a.Language == "deu").ToList();

        var record = new Series
        {
            SeriesId = tvdbId,
            Name = series.Name,
            GermanName = germanName,
            Aliases = JsonSerializer.Serialize(germanAliases),
            LastUpdated = series.LastUpdated ?? DateTime.Today, // Dont really care about this but lets set it to today as a secondary marker of when we've cached this
            NextAired = series.NextAired.ToDateTime(),
            LastAired = series.LastAired.ToDateTime(),
            CacheExpiry = DateTime.Today.AddHours(Constants.MediathekArrConstants.MediathekArr_DatabaseCache_Expiry),
            Episodes = [.. series.Episodes.Select(e => new Episode
            {
                Id = e.Id,
                SeriesId = tvdbId,
                Name = e.Name,
                Aired = e.Aired,
                Runtime = e.Runtime,
                SeasonNumber = e.SeasonNumber,
                EpisodeNumber = e.Number
            })]
        };

        /* Clear existing cache in DB and then recreate one cached item */
        Context.Series.RemoveRange(Context.Series.Where(s => s.SeriesId == tvdbId));
        await Context.Series.AddAsync(record);
        await Context.SaveChangesAsync();
        Logger.LogInformation("Added Series {tvdbId} from TVDB into the Cache.", tvdbId);

        /* Clear existing cache in Memory and then recreate one cached item */
        var cacheKey = $"Series_{tvdbId}";
        MemoryCache.Remove(cacheKey);
        MemoryCache.Set(cacheKey, record, TimeSpan.FromHours(Constants.MediathekArrConstants.MediathekArr_MemoryCache_Expiry));

        return record;
    }
}
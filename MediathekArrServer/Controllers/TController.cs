using MediathekArr.Models.Newznab;
using MediathekArr.Utilities;
using MediathekArr.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace MediathekArr.Controllers;

[ApiController]
[Route("api")]
public class TController(MediathekSearchService mediathekSearchService, ItemLookupService itemLookupService, MovieLookupService movieLookupService) : ControllerBase
{
    private readonly MediathekSearchService _mediathekSearchService = mediathekSearchService;
    private readonly ItemLookupService _itemLookupService = itemLookupService;
    private readonly MovieLookupService _movieLookupService = movieLookupService;

    [HttpGet]
    public async Task<IActionResult> GetCapsXml([FromQuery] string t)
    {
        var limit = int.TryParse(HttpContext.Request.Query["limit"], out var parsedLimit) ? parsedLimit : 100;
        var offset = int.TryParse(HttpContext.Request.Query["offset"], out var parsedOffset) ? parsedOffset: 0;
        var hideNoMatch = bool.TryParse(HttpContext.Request.Query["hideNoMatch"], out var parsedHideNoMatch) && parsedHideNoMatch;
        var hideUncertainMatch = bool.TryParse(HttpContext.Request.Query["hideUncertainMatch"], out var parsedHideUncertainMatch) && parsedHideUncertainMatch;
        var matchConfidenceFilter = new MatchConfidenceFilter(hideNoMatch, hideUncertainMatch);
        string q = HttpContext.Request.Query["q"];
        string imdbid = HttpContext.Request.Query["imdbid"];
        string tvdbid = HttpContext.Request.Query["tvdbid"];
        string tmdbid = HttpContext.Request.Query["tmdbid"];
        string season = HttpContext.Request.Query["season"];
        string episode = HttpContext.Request.Query["ep"];
        string cat = HttpContext.Request.Query["cat"];

        if (t == "caps")
        {
            string xmlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<caps>
    <limits max=""5000"" default=""5000""/>
    <registration available=""no"" open=""no""/>
    <searching>
        <search available=""yes"" supportedParams=""q""/>
        <tv-search available=""yes"" supportedParams=""q,season,ep,tvdbid""/>
        <movie-search available=""yes"" supportedParams=""q,imdbid""/>
        <audio-search available=""no"" supportedParams="""" />
    </searching>
    <categories>
        <category id=""2000"" name=""Movies"">
            <subcat id=""2040"" name=""HD""/>
            <subcat id=""2030"" name=""SD""/>
        </category>
        <category id=""5000"" name=""TV"">
            <subcat id=""5040"" name=""HD""/>
            <subcat id=""5030"" name=""SD""/>
        </category>
    </categories>
</caps>";

            return Content(xmlContent, "application/xml", Encoding.UTF8);
        }
        else if (t == "tvsearch" || t == "search" || t == "movie")
        {   
            try
            {
                // Movie search: resolve the IMDb id to titles/year and match films only
                if (t == "movie" && (!string.IsNullOrEmpty(imdbid) || !string.IsNullOrEmpty(q)))
                {
                    var movieInfo = await _movieLookupService.GetMovieInfoByImdbId(imdbid);
                    if (movieInfo is null && string.IsNullOrEmpty(q))
                    {
                        // unknown id and no text: nothing to search for (never return the whole feed)
                        return Content(NewznabUtils.SerializeRss(NewznabUtils.GetEmptyRssResult()), "application/xml", Encoding.UTF8);
                    }

                    string movieResults = await _mediathekSearchService.FetchMovieSearchResults(movieInfo, q, limit, offset, matchConfidenceFilter);
                    return Content(movieResults, "application/xml", Encoding.UTF8);
                }

                // ids we don't support (tmdbid, unparsable tvdbid) without any text: empty result instead of everything
                if (string.IsNullOrEmpty(q) && string.IsNullOrEmpty(season) &&
                    (!string.IsNullOrEmpty(tmdbid) || !string.IsNullOrEmpty(imdbid) || (!string.IsNullOrEmpty(tvdbid) && !int.TryParse(tvdbid, out _))))
                {
                    return Content(NewznabUtils.SerializeRss(NewznabUtils.GetEmptyRssResult()), "application/xml", Encoding.UTF8);
                }

                if (!string.IsNullOrEmpty(tvdbid) && int.TryParse(tvdbid, out var parsedTvdbid))
                {
                    var tvdbData = await _itemLookupService.GetShowInfoByTvdbId(parsedTvdbid);

                    string searchResults = await _mediathekSearchService.FetchSearchResultsFromApiById(tvdbData, season, episode, limit, offset, matchConfidenceFilter);

                    return Content(searchResults, "application/xml", Encoding.UTF8);
                }
                else if (q is null && season is null && imdbid is null && tvdbid is null && tmdbid is null)
                {
                    string searchResults = await _mediathekSearchService.FetchSearchResultsForRssSync(limit, offset, matchConfidenceFilter);
                    return Content(searchResults, "application/xml", Encoding.UTF8);
                }
                else
                {
                    string searchResults = await _mediathekSearchService.FetchSearchResultsFromApiByString(q, season, limit, offset, matchConfidenceFilter);
                    return Content(searchResults, "application/xml", Encoding.UTF8);
                }
            }
            catch (HttpRequestException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        return NotFound();
    }


    [HttpGet("fake_nzb_download")]
    public IActionResult FakeNzbDownload([FromQuery] string encodedVideoUrl, [FromQuery] string? encodedSubtitleUrl, [FromQuery] string encodedTitle)
    {
        var nzbContent = $@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<!DOCTYPE nzb PUBLIC ""-//newzBin//DTD NZB 1.0//EN"" ""http://www.newzbin.com/DTD/nzb/nzb-1.0.dtd"">
<!-- {encodedTitle} -->
<!-- {encodedVideoUrl} -->
<!-- {encodedSubtitleUrl ?? string.Empty} -->
<nzb>
    <file post_id=""1"">
        <groups>
            <group>a.b.zdf</group>
        </groups>
        <segments>
            <segment number=""1"">ExampleSegmentID@news.example.com</segment>
        </segments>
    </file>
</nzb>";

        // Convert the NZB XML content to byte array
        var fileContent = Encoding.UTF8.GetBytes(nzbContent);

        // Set the .nzb file name
        var nzbFileName = $"mediathek-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.nzb";

        return File(fileContent, "application/x-nzb", nzbFileName);
    }
}

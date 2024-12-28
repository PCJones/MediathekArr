using MediathekArrServer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace MediathekArrServer.Controllers;

[ApiController]
[Route("api")]
public class TController(MediathekSearchService mediathekSearchService, ItemLookupService itemLookupService) : ControllerBase
{
    private readonly MediathekSearchService _mediathekSearchService = mediathekSearchService;
    private readonly ItemLookupService _itemLookupService = itemLookupService;

    [HttpGet]
    public async Task<IActionResult> GetCapsXml([FromQuery] string t)
    {
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
                if (!string.IsNullOrEmpty(tvdbid) && int.TryParse(tvdbid, out var parsedTvdbid))
                {
                    var tvdbData = await _itemLookupService.GetShowInfoByTvdbId(parsedTvdbid);

                    string searchResults = await _mediathekSearchService.FetchSearchResultsFromApiById(tvdbData, season, episode);

                    return Content(searchResults, "application/xml", Encoding.UTF8);
                }
                else if (q is null && season is null && imdbid is null && tvdbid is null && tmdbid is null)
                {
                    string searchResults = await _mediathekSearchService.FetchSearchResultsForRssSync(0, 0);
                    return Content(searchResults, "application/xml", Encoding.UTF8);
                }
                else
                {
                    string searchResults = await _mediathekSearchService.FetchSearchResultsFromApiByString(q, season);
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
    public IActionResult FakeNzbDownload([FromQuery] string encodedUrl, [FromQuery] string encodedTitle)
    {
        string decodedUrl;
        string decodedTitle;
        try
            {
            var base64EncodedBytesUrl = Convert.FromBase64String(encodedUrl);
            decodedUrl = Encoding.UTF8.GetString(base64EncodedBytesUrl);
            var base64EncodedBytesTitle = Convert.FromBase64String(encodedTitle);
            decodedTitle = Encoding.UTF8.GetString(base64EncodedBytesTitle);
        }
        catch (FormatException)
        {
            return BadRequest("Invalid base64 string.");
        }

        // Define a basic NZB XML structure with the comment and encoded URL.
        var nzbContent = $@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<!DOCTYPE nzb PUBLIC ""-//newzBin//DTD NZB 1.0//EN"" ""http://www.newzbin.com/DTD/nzb/nzb-1.0.dtd"">
<!-- {decodedTitle} -->
<!-- {decodedUrl} -->
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

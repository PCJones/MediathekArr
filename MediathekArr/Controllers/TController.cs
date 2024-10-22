using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using MediathekArr.Models;
using System.IO;
using Guid = MediathekArr.Models.Guid;
using System.Reflection.Metadata;
using MediathekArr.Services;

namespace MediathekArr.Controllers
{
    [ApiController]
    [Route("api")]
    public class TController : ControllerBase
    {
        private readonly MediathekSearchService _mediathekSearchService;

        public TController(MediathekSearchService mediathekSearchService)
        {
            _mediathekSearchService = mediathekSearchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCapsXml([FromQuery] string t)
        {
            string q = HttpContext.Request.Query["q"];
            string imdbid = HttpContext.Request.Query["imdbid"];
            string tvdbid = HttpContext.Request.Query["tvdbid"];
            string season = HttpContext.Request.Query["season"];
            string episode = HttpContext.Request.Query["ep"];

            if (t == "caps")
            {
                string xmlContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<caps>
    <limits max=""100"" default=""100""/>
    <registration available=""no"" open=""no""/>
    <searching>
        <search available=""no"" supportedParams=""q""/>
        <tv-search available=""yes"" supportedParams=""q,season,ep""/>
        <movie-search available=""yes"" supportedParams=""q""/>
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
            else if (t == "tvsearch")
            {
                // Check if 'q' parameter is set
                if (string.IsNullOrWhiteSpace(q))
                {
                    // if Indexer check return dummy result so result isn't empty
                    if (string.IsNullOrWhiteSpace(season) && string.IsNullOrWhiteSpace(episode) && string.IsNullOrWhiteSpace(tvdbid))
                    {
                        return Content(GetTvSearchDummyData(), "application/xml", Encoding.UTF8);
                    }

                    // Otherwise return an empty result for tvsearch if 'q' is not provided
                    return Content(GetTvSearchEmptyResult(), "application/xml", Encoding.UTF8);
                }

                string searchResults = await _mediathekSearchService.FetchSearchResultsFromApi(q, season);

                return Content(searchResults, "application/xml", Encoding.UTF8);
            }

            return NotFound();
        }

        private string GetTvSearchDummyData()
        {
            var rss = new Rss
            {
                Channel = new Channel
                {
                    Title = "MediathekArr",
                    Description = "MediathekArr API results",
                    Response = new NewznabResponse
                    {
                        Offset = 0,
                        Total = 1234
                    },
                    Items = new List<Item>
                    {
                        new Item
                        {
                            Title = "Dummy.Result.S06E05",
                            Guid = new Guid
                            {
                                IsPermaLink = true,
                                Value = "http://example.com/rss/viewnzb/e9c515e02346086e3a477a5436d7bc8c"
                            },
                            Link = "http://example.com/rss/nzb/e9c515e02346086e3a477a5436d7bc8c&amp;i=1&amp;r=18cf9f0a736041465e3bd521d00a90b9",
                            Comments = "http://example.com/rss/viewnzb/e9c515e02346086e3a477a5436d7bc8c#comments",
                            PubDate = "Sun, 06 Jun 2010 17:29:23 +0100",
                            Category = "TV > HD",
                            Description = "Some TV show",
                            Enclosure = new Enclosure
                            {
                                Url = "http://example.com/rss/nzb/e9c515e02346086e3a477a5436d7bc8c&amp;i=1&amp;r=18cf9f0a736041465e3bd521d00a90b9",
                                Length = 154653309,
                                Type = "application/x-nzb"
                            },
                            Attributes = new List<NewznabAttribute>
                            {
                                new NewznabAttribute { Name = "category", Value = "5040" },
                                new NewznabAttribute { Name = "size", Value = "154653309" },
                                new NewznabAttribute { Name = "season", Value = "3" },
                                new NewznabAttribute { Name = "episode", Value = "2" }
                            }
                        }
                    }
                }
            };

            // Serialize the object to XML
            var serializer = new XmlSerializer(typeof(Rss));
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, rss);
                return stringWriter.ToString();
            }
        }

        private string GetTvSearchEmptyResult()
        {
            return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<tvsearch>
    <results>
        <total>0</total>
        <items/>
    </results>
</tvsearch>";
        }
    }
}

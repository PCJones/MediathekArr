using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MediathekArrDownloader.Controllers;

[ApiController]
[Route("[controller]")]
public class WizardController : ApiProxyControllerBase
{
    public WizardController(IHttpClientFactory httpClientFactory)
        : base(httpClientFactory)
    {
    }

    [HttpGet("downloadclients")]
    public async Task<IActionResult> GetDownloadClients([FromQuery] string apiKey, [FromQuery] string sonarrHost)
    {
        return await ExecuteApiRequest(
            apiKey,
            sonarrHost,
            "api/v3/downloadclient",
            HttpMethod.Get,
            "fetching download clients",
            responseContent =>
            {
                var clients = JsonConvert.DeserializeObject<List<JObject>>(responseContent);

                var filteredClients = clients
                    .Where(client =>
                        (string)client["implementation"] == "Sabnzbd" &&
                        client["fields"].Any(field => (string)field["name"] == "urlBase" && (string)field["value"] == "download"))
                    .Select(client => new
                    {
                        Id = (int)client["id"],
                        Name = (string)client["name"],
                        Priority = (string)client["priority"],
                        Host = client["fields"].FirstOrDefault(field => (string)field["name"] == "host")?["value"]?.ToString(),
                        Port = client["fields"].FirstOrDefault(field => (string)field["name"] == "port")?["value"]?.ToString(),
                        Category = client["fields"].FirstOrDefault(field => (string)field["name"] == "tvCategory")?["value"]?.ToString(),
                        Enable = (bool)client["enable"]
                    })
                    .ToList();

                return Ok(filteredClients);
            },
            "Sonarr host");
    }

    [HttpPost("downloadclient")]
    public async Task<IActionResult> AddDownloadClient([FromQuery] string apiKey, [FromQuery] string sonarrHost)
    {
        return await ExecuteApiRequest(
            apiKey,
            sonarrHost,
            "api/v3/downloadclient",
            HttpMethod.Post,
            "adding download client",
            hostName: "Sonarr host");
    }

    [HttpPut("downloadclient/{id}")]
    public async Task<IActionResult> UpdateDownloadClient([FromQuery] string apiKey, [FromQuery] string sonarrHost, [FromRoute] int id)
    {
        return await ExecuteApiRequest(
            apiKey,
            sonarrHost,
            $"api/v3/downloadclient/{id}",
            HttpMethod.Put,
            "updating download client",
            hostName: "Sonarr host");
    }

    [HttpGet("indexers")]
    public async Task<IActionResult> GetIndexers([FromQuery] string apiKey, [FromQuery] string arrHost, [FromQuery] int portFilter = 5007, [FromQuery] bool prowlarr = false)
    {
        string apiVersion = prowlarr ? "v1" : "v3";

        return await ExecuteApiRequest(
            apiKey,
            arrHost,
            $"api/{apiVersion}/indexer",
            HttpMethod.Get,
            "fetching indexers",
            responseContent =>
            {
                var indexers = JsonConvert.DeserializeObject<List<JObject>>(responseContent);

                var filteredIndexers = indexers
                    .Where(indexer =>
                    {
                        if (prowlarr)
                        {
                            var indexerUrls = indexer["indexerUrls"]?.ToObject<List<string>>();
                            return indexerUrls != null && indexerUrls.Any(url => url.Contains($":{portFilter}")) &&
                                   (string)indexer["protocol"] == "usenet";
                        }
                        else
                        {
                            return (string)indexer["protocol"] == "usenet" &&
                                   indexer["fields"].Any(field =>
                                       (string)field["name"] == "baseUrl" && ((string)field["value"]).Contains($":{portFilter}"));
                        }
                    })
                    .Select(indexer =>
                    {
                        var baseUrl = prowlarr ?
                             indexer["indexerUrls"]?.ToObject<List<string>>().FirstOrDefault(url => url.Contains($":{portFilter}")) :
                            indexer["fields"]?.FirstOrDefault(field => (string)field["name"] == "baseUrl")?["value"]?.ToString();

                        return new
                        {
                            Id = (int)indexer["id"],
                            Name = (string)indexer["name"],
                            Priority = (int)indexer["priority"],
                            DownloadClientId = (int)indexer["downloadClientId"],
                            BaseUrl = baseUrl,
                            ApiPath = indexer["fields"]?.FirstOrDefault(field => (string)field["name"] == "apiPath")?["value"]?.ToString(),
                            EnableRss = prowlarr ? (bool)indexer["supportsRss"] : (bool)indexer["enableRss"],
                            EnableAutomaticSearch = prowlarr ? (bool)indexer["supportsSearch"] : (bool)indexer["enableAutomaticSearch"],
                            EnableInteractiveSearch = prowlarr ? true : (bool)indexer["enableInteractiveSearch"]
                        };
                    })
                    .ToList();

                return Ok(filteredIndexers);
            },
            "Host");
    }

    [HttpPost("indexer")]
    public async Task<IActionResult> AddIndexer([FromQuery] string apiKey, [FromQuery] string arrHost, [FromQuery] bool prowlarr = false)
    {
        string apiVersion = prowlarr ? "v1" : "v3";

        return await ExecuteApiRequest(
            apiKey,
            arrHost,
            $"api/{apiVersion}/indexer",
            HttpMethod.Post,
            "adding indexer");
    }

    [HttpPut("indexer/{id}")]
    public async Task<IActionResult> UpdateIndexer([FromQuery] string apiKey, [FromQuery] string arrHost, [FromRoute] int id, [FromQuery] bool prowlarr = false)
    {
        string apiVersion = prowlarr ? "v1" : "v3";

        return await ExecuteApiRequest(
            apiKey,
            arrHost,
            $"api/{apiVersion}/indexer/{id}",
            HttpMethod.Put,
            "updating indexer");
    }

    [HttpPost("indexer/test")]
    public async Task<IActionResult> TestIndexerSettings([FromQuery] string apiKey, [FromQuery] string arrHost, [FromQuery] bool prowlarr = false)
    {
        string apiVersion = prowlarr ? "v1" : "v3";

        return await ExecuteApiRequest(
            apiKey,
            arrHost,
            $"api/{apiVersion}/indexer/test",
            HttpMethod.Post,
            "testing indexer");
    }

    [HttpPost("downloadclient/test")]
    public async Task<IActionResult> TestDownloadClientSettings([FromQuery] string apiKey, [FromQuery] string sonarrHost)
    {
        return await ExecuteApiRequest(
            apiKey,
            sonarrHost,
            "api/v3/downloadclient/test",
            HttpMethod.Post,
            "testing download client",
            hostName: "Sonarr host");
    }

    [HttpGet("appprofiles")]
    public async Task<IActionResult> GetAppProfiles([FromQuery] string apiKey, [FromQuery] string prowlarrHost)
    {
        return await ExecuteApiRequest(
            apiKey,
            prowlarrHost,
            "api/v1/appprofile",
            HttpMethod.Get,
            "fetching app profiles",
            hostName: "Prowlarr host");
    }
}
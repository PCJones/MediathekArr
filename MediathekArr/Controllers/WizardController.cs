using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MediathekArrDownloader.Controllers;

[ApiController]
[Route("[controller]")]
public class WizardController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet("downloadclients")]
    public async Task<IActionResult> GetDownloadClients([FromQuery] string apiKey, [FromQuery] string sonarrHost)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sonarrHost))
        {
            return BadRequest("Sonarr host and API key are required.");
        }

        var cleanedHostName = sonarrHost.TrimEnd('/');

        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        try
        {
            
            var response = await httpClient.GetAsync($"{cleanedHostName}/api/v3/downloadclient");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var clients = JsonConvert.DeserializeObject<List<JObject>>(content);

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
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching clients: {ex.Message}");
        }
    }

    [HttpPost("downloadclient")]
    public async Task<IActionResult> AddDownloadClient([FromQuery] string apiKey, [FromQuery] string sonarrHost, [FromBody] JObject newClient)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sonarrHost))
        {
            return BadRequest("Sonarr host and API key are required.");
        }

        var cleanedHostName = sonarrHost.TrimEnd('/');

        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        try
        {
            var content = new StringContent(newClient.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{cleanedHostName}/api/v3/downloadclient", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdClient = JsonConvert.DeserializeObject<JObject>(responseContent);

            return Ok(createdClient);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error adding client: {ex.Message}");
        }
    }

    [HttpPut("downloadclient/{id}")]
    public async Task<IActionResult> UpdateDownloadClient([FromQuery] string apiKey, [FromQuery] string sonarrHost, [FromRoute] int id, [FromBody] JObject updatedClient)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sonarrHost))
        {
            return BadRequest("Sonarr host and API key are required.");
        }

        var cleanedHostName = sonarrHost.TrimEnd('/');

        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        try
        {
            var content = new StringContent(updatedClient.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync($"{cleanedHostName}/api/v3/downloadclient/{id}", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedClientResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            return Ok(updatedClientResponse);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating client: {ex.Message}");
        }
    }

    [HttpGet("indexers")]
    public async Task<IActionResult> GetIndexers([FromQuery] string apiKey, [FromQuery] string arrHost, [FromQuery] bool prowlarr = false)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(arrHost))
        {
            return BadRequest("Host and API key are required.");
        }

        var cleanedHostName = arrHost.TrimEnd('/');
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        try
        {
            // Adjust API version for Prowlarr
            var apiVersion = prowlarr ? "v1" : "v3";
            var response = await httpClient.GetAsync($"{cleanedHostName}/api/{apiVersion}/indexer");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var indexers = JsonConvert.DeserializeObject<List<JObject>>(content);

            var filteredIndexers = indexers
                .Where(indexer =>
                {
                    if (prowlarr)
                    {
                        // Check indexerUrls for Prowlarr
                        var indexerUrls = indexer["indexerUrls"]?.ToObject<List<string>>();
                        return indexerUrls != null && indexerUrls.Any(url => url.Contains(":5007")) &&
                               (string)indexer["protocol"] == "usenet";
                    }
                    else
                    {
                        // Default (Sonarr/Radarr) logic
                        return (string)indexer["protocol"] == "usenet" &&
                               indexer["fields"].Any(field =>
                                   (string)field["name"] == "baseUrl" && ((string)field["value"]).Contains(":5007"));
                    }
                })
                .Select(indexer =>
                {
                    // Adapt fields for Prowlarr
                    var baseUrl = prowlarr ?
                         indexer["indexerUrls"]?.ToObject<List<string>>().FirstOrDefault(url => url.Contains(":5007")) :
                        indexer["fields"]?.FirstOrDefault(field => (string)field["name"] == "baseUrl")?["value"]?.ToString();

                    return new
                    {
                        Id = (int)indexer["id"],
                        Name = (string)indexer["name"],
                        Priority = (int)indexer["priority"],
                        DownloadClientId = (int)indexer["downloadClientId"],
                        BaseUrl = baseUrl,
                        ApiPath = indexer["fields"]?.FirstOrDefault(field => (string)field["name"] == "apiPath")?["value"]?.ToString(),
                        EnableRss = prowlarr? (bool)indexer["supportsRss"] : (bool)indexer["enableRss"],
                        EnableAutomaticSearch = prowlarr? (bool)indexer["supportsSearch"] : (bool)indexer["enableAutomaticSearch"],
                        EnableInteractiveSearch = prowlarr ? true : (bool)indexer["enableInteractiveSearch"]
                    };
                })
                .ToList();

            return Ok(filteredIndexers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching indexers: {ex.Message}");
        }
    }
}

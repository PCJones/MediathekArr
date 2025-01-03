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
    public async Task<IActionResult> AddDownloadClient([FromQuery] string apiKey, [FromQuery] string sonarrHost)
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
            // Read the raw body content
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            // Forward the raw payload to the Sonarr API
            var content = new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{cleanedHostName}/api/v3/downloadclient", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdClient = JsonConvert.DeserializeObject<JObject>(responseContent);

            return Ok(createdClient);
        }
        catch (JsonReaderException ex)
        {
            return BadRequest($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error adding client: {ex.Message}");
        }
    }

    [HttpPut("downloadclient/{id}")]
    public async Task<IActionResult> UpdateDownloadClient([FromQuery] string apiKey, [FromQuery] string sonarrHost, [FromRoute] int id)
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
            // Read the raw body content
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            // Forward the raw payload to the Sonarr API
            var content = new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync($"{cleanedHostName}/api/v3/downloadclient/{id}", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedClientResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            return Ok(updatedClientResponse);
        }
        catch (JsonReaderException ex)
        {
            return BadRequest($"Invalid JSON format: {ex.Message}");
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
                        EnableRss = prowlarr ? (bool)indexer["supportsRss"] : (bool)indexer["enableRss"],
                        EnableAutomaticSearch = prowlarr ? (bool)indexer["supportsSearch"] : (bool)indexer["enableAutomaticSearch"],
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

    [HttpPost("indexer")]
    public async Task<IActionResult> AddIndexer([FromQuery] string apiKey, [FromQuery] string arrHost, [FromQuery] bool prowlarr = false)
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
            // Read the raw body content
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            // Forward the raw payload to the API
            var apiVersion = prowlarr ? "v1" : "v3";
            var content = new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{cleanedHostName}/api/{apiVersion}/indexer", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdIndexer = JsonConvert.DeserializeObject<JObject>(responseContent);

            return Ok(createdIndexer);
        }
        catch (JsonReaderException ex)
        {
            return BadRequest($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error adding indexer: {ex.Message}");
        }
    }

    [HttpPut("indexer/{id}")]
    public async Task<IActionResult> UpdateIndexer([FromQuery] string apiKey, [FromQuery] string arrHost, [FromRoute] int id, [FromQuery] bool prowlarr = false)
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
            // Read the raw body content
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            // Forward the raw payload to the API
            var apiVersion = prowlarr ? "v1" : "v3";
            var content = new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PutAsync($"{cleanedHostName}/api/{apiVersion}/indexer/{id}", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var updatedIndexerResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            return Ok(updatedIndexerResponse);
        }
        catch (JsonReaderException ex)
        {
            return BadRequest($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating indexer: {ex.Message}");
        }
    }

    [HttpPost("downloadclient/test")]
    public async Task<IActionResult> TestDownloadClientSettings([FromQuery] string apiKey, [FromQuery] string sonarrHost)
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
            // Read the raw body content
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            // Parse the raw body as JObject
            var payload = JObject.Parse(rawBody);

            // Forward the raw payload to Sonarr
            var content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{cleanedHostName}/api/v3/downloadclient/test", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var testResult = JsonConvert.DeserializeObject<JObject>(responseContent);

            return Ok(testResult);
        }
        catch (JsonReaderException ex)
        {
            return BadRequest($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error testing download client settings: {ex.Message}");
        }
    }


    [HttpPost("indexer/test")]
    public async Task<IActionResult> TestIndexerSettings([FromQuery] string apiKey, [FromQuery] string arrHost, [FromQuery] bool prowlarr = false)
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
            // Read the raw body content
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            // Forward the raw payload to the API
            var apiVersion = prowlarr ? "v1" : "v3";
            var content = new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{cleanedHostName}/api/{apiVersion}/indexer/test", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var testResult = JsonConvert.DeserializeObject<JObject>(responseContent);

            return Ok(testResult);
        }
        catch (JsonReaderException ex)
        {
            return BadRequest($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error testing indexer settings: {ex.Message}");
        }
    }

    [HttpGet("appprofiles")]
    public async Task<IActionResult> GetAppProfiles([FromQuery] string apiKey, [FromQuery] string prowlarrHost, [FromQuery] string prowlarrPort)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(prowlarrHost) || string.IsNullOrWhiteSpace(prowlarrPort))
        {
            return BadRequest("Prowlarr host, port, and API key are required.");
        }

        var cleanedHostName = $"{prowlarrHost.TrimEnd('/')}:{prowlarrPort}";
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        try
        {
            var response = await httpClient.GetAsync($"{cleanedHostName}/api/v1/appprofile");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var appProfiles = JsonConvert.DeserializeObject<List<JObject>>(content);

            return Ok(appProfiles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching app profiles: {ex.Message}");
        }
    }


}

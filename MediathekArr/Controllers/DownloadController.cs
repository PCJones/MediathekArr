using MediathekArr.Models;
using MediathekArr.Models.SABnzbd;
using MediathekArr.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MediathekArr.Controllers;

[ApiController]
[Route("[controller]")]
public partial class DownloadController(DownloadService downloadService, Config config) : ControllerBase
{
    private readonly DownloadService _downloadService = downloadService;
    private readonly Config _config = config;

    [HttpGet("api")]
    public IActionResult GetVersion([FromQuery] string mode, [FromQuery] string? name = null, [FromQuery] string? value = null, [FromQuery] int? del_files = 0)
    {
        return mode switch
        {
            "version" => Ok(new { version = "4.3.3" }),
            "get_config" => Content(ConfigResponse, "application/json"),
            "fullstatus" => Content(FullStatusResponse, "application/json"),
            "translate" => (value == "ping") ? Ok(new { value = "pong" }) : Ok(new { value = value }),
            "queue" => Ok(GetQueue()),
            "history" => (name == "delete" && !string.IsNullOrEmpty(value))
                ? DeleteHistoryItem(value, del_files.GetValueOrDefault() == 1)
                : Ok(GetHistory()),
            _ => BadRequest(new { error = "Invalid mode" }),
        };
    }

    private IActionResult DeleteHistoryItem(string nzoId, bool delFiles)
    {
        // Call the DeleteHistoryItem method in the service
        bool isDeleted = _downloadService.DeleteHistoryItem(nzoId, delFiles);

        // Return success or failure response based on deletion result
        return isDeleted
            ? Ok(new { status = true })
            : NotFound(new { status = false, error = "Item not found" });
    }

    [HttpPost("api")]
    public async Task<IActionResult> AddFileEndpoint([FromQuery] string mode, [FromQuery] string cat, [FromQuery] string? name)
    {
        if (!_config.Categories.Contains(cat))
        {
            return BadRequest(new { error = "Invalid category" });
        }

        if (mode == "addfile")
        {
            return await AddFileByNzb(cat);
        }
        else if(mode == "addurl" && !string.IsNullOrWhiteSpace(name))
        {
            return await AddFileByUrl(cat, name);
        }
        else
        {
            return BadRequest(new { error = "Invalid mode" });
        }

    }

    private async Task<IActionResult> AddFileByUrl(string cat, string name)
    {
        var uri = new Uri(name);
        var query = HttpUtility.ParseQueryString(uri.Query);
        string? encodedVideoUrl = query["encodedVideoUrl"];
        if (string.IsNullOrEmpty(encodedVideoUrl))
        {
            return BadRequest(new { error = "Missing encodedVideoUrl parameter" });
        }

        string encodedTitle = query["encodedTitle"]?.Trim() ?? string.Empty;
        string encodedSubtitleUrl = query["encodedSubtitleUrl"] ?? string.Empty;
        var base64EncodedBytesVideoUrl = Convert.FromBase64String(encodedVideoUrl);
        string decodedVideoUrl = Encoding.UTF8.GetString(base64EncodedBytesVideoUrl);
        var base64EncodedBytesTitle = Convert.FromBase64String(encodedTitle);
        string decodedTitle = Encoding.UTF8.GetString(base64EncodedBytesTitle);
        string decodedSubtitleUrl;
        if (string.IsNullOrEmpty(encodedSubtitleUrl))
        {
            decodedSubtitleUrl = string.Empty;
        }
        else
        {
            var base64EncodedBytesSubtitleUrl = Convert.FromBase64String(encodedSubtitleUrl);
            decodedSubtitleUrl = Encoding.UTF8.GetString(base64EncodedBytesSubtitleUrl);
        }

        if (!decodedVideoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !decodedVideoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid video URL scheme" });
        }

        if (!string.IsNullOrEmpty(decodedSubtitleUrl) &&
            !decodedSubtitleUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !decodedSubtitleUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Invalid subtitle URL scheme" });
        }

        var queueItem = _downloadService.AddToQueue(decodedVideoUrl, decodedSubtitleUrl, decodedTitle, cat);

        return Ok(new
        {
            status = true,
            nzo_ids = new[] { queueItem.Id }
        });
    }

    private async Task<IActionResult> AddFileByNzb(string cat)
    {
        // Read the fake NZB file from the request body
        using var reader = new StreamReader(Request.Body);
        var requestBody = await reader.ReadToEndAsync();

        string[] lines = requestBody.Split(Environment.NewLine);

        var filenameMatch = FileNameRegex().Match(lines[6]);
        var videoUrlMatch = UrlRegex().Match(lines[7]);
        var subtitleUrlMatch = UrlRegex().Match(lines[8]);

        if (!filenameMatch.Success || !videoUrlMatch.Success)
        {
            return BadRequest(new { error = "Invalid NZB format" });
        }

        var fileName = $"{filenameMatch.Groups[1].Value.Trim()}";
        var videoDownloadUrl = videoUrlMatch.Groups[1].Value;
        var subtitleDownloadUrl = subtitleUrlMatch.Groups[1].Value;

        // Add to the download queue using DownloadService and capture the created queue item
        var queueItem = _downloadService.AddToQueue(videoDownloadUrl, subtitleDownloadUrl, fileName, cat);

        // Return response in the specified format
        return Ok(new
        {
            status = true,
            nzo_ids = new[] { queueItem.Id }
        });
    }

    private QueueWrapper GetQueue()
    {
        var queueItems = _downloadService.GetQueue();

        var queue = new Queue
        {
            Items = queueItems.ToList()
        };

        return new QueueWrapper
        {   
            Queue = queue
        };
    }

    private HistoryWrapper GetHistory()
    {
        var historyItems = _downloadService.GetHistory();

        var history = new History
        {
            Items = historyItems.ToList()
        };

        return new HistoryWrapper
        {
            History = history
        };
    }

    private string FullStatusResponse => @$"{{
       ""status"": {{
              ""completeDir"": ""{_config.CompletePath.Replace('\\', '/')}""
            }}
    }}";


    private string ConfigResponse
    {
        get
        {
            string completePathFixed = _config.CompletePath.Replace('\\', '/');

            var categoryEntries = new List<string>();
            foreach (var category in _config.Categories)
            {
                string dirPath = completePathFixed + "/" + category;
                categoryEntries.Add($@"{{
                    ""name"": ""{category}"",
                    ""pp"": """",
                    ""script"": ""Default"",
                    ""dir"": ""{dirPath}"",
                    ""priority"": -100
                }}");
            }

            string categoriesJson = string.Join(",\n", categoryEntries);

            return $@"{{
                ""config"": {{
                    ""misc"": {{
                        ""complete_dir"": ""{completePathFixed}"",
                        ""enable_tv_sorting"": false,
                        ""enable_movie_sorting"": false,
                        ""pre_check"": false,
                        ""history_retention"": """",
                        ""history_retention_option"": ""all""
                    }},
                    ""categories"": [
                        {categoriesJson}
                    ],
                    ""sorters"": []
                }}
            }}";
        }
    }

    [GeneratedRegex(@"<!--\s*([^<>]+)\s*-->")]
    private static partial Regex FileNameRegex();
    [GeneratedRegex(@"<!--\s*(https?://[^\s]+)\s*-->")]
    private static partial Regex UrlRegex();
}

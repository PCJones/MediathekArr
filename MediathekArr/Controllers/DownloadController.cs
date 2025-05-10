using MediathekArrDownloader.Models;
using MediathekArrDownloader.Models.SABnzbd;
using MediathekArrDownloader.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace MediathekArrDownloader.Controllers;

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
    public async Task<IActionResult> AddFile([FromQuery] string mode, [FromQuery] string cat)
    {
        if (mode != "addfile")
        {
            return BadRequest(new { error = "Invalid mode" });
        }

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
            nzo_ids = new[] { queueItem.Id}
        });
    }

    private QueueWrapper GetQueue()
    {
        var queueItems = _downloadService.GetQueue();

        var queue = new SabnzbdQueue
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

        var history = new SabnzbdHistory
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

﻿using MediathekArr.Models;
using MediathekArr.Services;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MediathekArr.Controllers;

[ApiController]
[Route("[controller]")]
public partial class DownloadController(DownloadService downloadService) : ControllerBase
{
    private readonly DownloadService _downloadService = downloadService;

    [HttpGet("api")]
    public IActionResult GetVersion([FromQuery] string mode, [FromQuery] string? name = null, [FromQuery] string? value = null, [FromQuery] int? del_files = 0)
    {
        return mode switch
        {
            "version" => Ok(new { version = "4.3.3" }),
            "get_config" => Content(GetConfigResponse(), "application/json"),
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

        var filenameMatch = FileNameRegex().Match(requestBody);
        var urlMatch = UrlRegex().Match(requestBody);

        if (!filenameMatch.Success || !urlMatch.Success)
        {
            return BadRequest(new { error = "Invalid NZB format" });
        }

        var fileName = filenameMatch.Groups[1].Value;
        var downloadUrl = urlMatch.Groups[1].Value;

        // Add to the download queue using DownloadService and capture the created queue item
        var queueItem = _downloadService.AddToQueue(downloadUrl, fileName, cat);

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
        var historytems = _downloadService.GetHistory();

        var history = new SabnzbdHistory
        {
            Items = historytems.ToList()
        };

        return new HistoryWrapper
        {
            History = history
        };
    }

    private static string GetConfigResponse()
    {
        var startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        var downloadFolderPathMapping = Environment.GetEnvironmentVariable("DOWNLOAD_FOLDER_PATH_MAPPING");

        var completeDir = !string.IsNullOrEmpty(downloadFolderPathMapping)
            ? Path.Combine(downloadFolderPathMapping)
            : Path.Combine(startupPath, "downloads"); ;

        return @$"{{
        ""config"": {{
            ""misc"": {{
                ""complete_dir"": ""{completeDir.Replace("\\", "/")}"",
                ""enable_tv_sorting"": false,
                ""enable_movie_sorting"": false,
                ""pre_check"": false,
                ""history_retention"": ""all""
            }},
            ""categories"": [
                {{
                    ""name"": ""sonarr"",
                    ""pp"": """",
                    ""script"": ""Default"",
                    ""dir"": """",
                    ""priority"": -100
                }},
                {{
                    ""name"": ""tv"",
                    ""pp"": """",
                    ""script"": ""Default"",
                    ""dir"": """",
                    ""priority"": -100
                }},
                {{
                    ""name"": ""radarr"",
                    ""pp"": """",
                    ""script"": ""Default"",
                    ""dir"": """",
                    ""priority"": -100
                }},
                {{
                    ""name"": ""movies"",
                    ""pp"": """",
                    ""script"": ""Default"",
                    ""dir"": """",
                    ""priority"": -100
                }},
                {{
                    ""name"": ""sonarr_blackhole"",
                    ""pp"": """",
                    ""script"": ""Default"",
                    ""dir"": """",
                    ""priority"": -100
                }},
                {{
                    ""name"": ""radarr_blackhole"",
                    ""pp"": """",
                    ""script"": ""Default"",
                    ""dir"": """",
                    ""priority"": -100
                }},
            ],
            ""sorters"": []
        }}
    }}";
    }

    [GeneratedRegex(@"filename=""([^""]+)\.nzb""")]
    private static partial Regex FileNameRegex();
    [GeneratedRegex(@"<!--\s*(https?://[^\s]+)\s*-->")]
    private static partial Regex UrlRegex();
}

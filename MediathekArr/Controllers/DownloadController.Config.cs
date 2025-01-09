using MediathekArr.Models;
using Microsoft.AspNetCore.Mvc;

namespace MediathekArr.Controllers;

public partial class DownloadController
{
    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var incompleteEnv = Environment.GetEnvironmentVariable("DOWNLOAD_INCOMPLETE_PATH");
        var completeEnv = Environment.GetEnvironmentVariable("DOWNLOAD_COMPLETE_PATH");
        
        var configDetails = new
        {
            config = _config,
            overrides = new
            {
                IncompletePath = !string.IsNullOrEmpty(incompleteEnv),
                CompletePath = !string.IsNullOrEmpty(completeEnv)
            }
        };

        return Ok(configDetails);
    }

    [HttpPost("config")]
    public IActionResult UpdateConfig([FromBody] Config newConfig)
    {
        var incompleteEnv = Environment.GetEnvironmentVariable("DOWNLOAD_INCOMPLETE_PATH");
        var completeEnv = Environment.GetEnvironmentVariable("DOWNLOAD_COMPLETE_PATH");

        // Prevent updates to fields overridden by environment variables
        if (string.IsNullOrEmpty(incompleteEnv))
        {
            _config.IncompletePath = newConfig.IncompletePath;
        }

        if (string.IsNullOrEmpty(completeEnv))
        {
            _config.CompletePath = newConfig.CompletePath;
        }

        // Persist updated config to file
        var configPathEnv = Environment.GetEnvironmentVariable("CONFIG_PATH");

        string configFilePath;
        if (!string.IsNullOrEmpty(configPathEnv))
        {
            configFilePath = Path.Combine(configPathEnv, "mediathekarr.json");
        }
        else
        {
            configFilePath = Path.Combine("config", "mediathekarr.json");
            if (!Directory.Exists("config"))
            {
                Directory.CreateDirectory("config");
            }
        }
        System.IO.File.WriteAllText(configFilePath, System.Text.Json.JsonSerializer.Serialize(_config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        return Ok(new { status = "success" });
    }

    [HttpGet("browse")]
    public IActionResult BrowsePath([FromQuery] string path = "")
    {
        try
        {
            string basePath = string.IsNullOrEmpty(path) ? "/" : path;

            if (basePath == "/" && OperatingSystem.IsWindows())
            {
                // If the path is "/" on Windows, return the available drives
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => new { d.Name, Path = d.Name.Replace("\\", "/") });

                return Ok(new
                {
                    currentPath = basePath,
                    directories = drives
                });
            }

            // Get directory contents
            var directories = Directory.GetDirectories(basePath)
                .Select(d => new { Name = Path.GetFileName(d), Path = d.Replace("\\", "/") });

            return Ok(new
            {
                currentPath = basePath,
                directories
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while browsing.", details = ex.Message });
        }
    }


}

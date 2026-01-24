using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace MediathekArr.Utilities;

public static class FfmpegUtils
{
    public static string GetFfmpegArguments(string url, string subtitlePath, string mkvPath, bool subtitlesAvailable)
    {
        var subInput = "";
        var subMap = "";

        if (subtitlesAvailable)
        {
            subInput = $"-i \"{subtitlePath}\" ";
            subMap = "-map 1:0 -c:s srt -metadata:s:s:0 language=ger ";
        }

        return $"-i \"{url}\" {subInput}-map 0:v -map 0:a {subMap}-c:v copy -c:a copy -metadata:s:v:0 language=ger -metadata:s:a:0 language=ger \"{mkvPath}\"";
    }

    public static async Task EnsureFfmpegExistsAsync(string ffmpegPath, ILogger logger)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        
        if (Path.IsPathRooted(ffmpegPath))
        {
            if (File.Exists(ffmpegPath))
            {
                logger.LogInformation("ffmpeg found at {FfmpegPath}.", ffmpegPath);
                return;
            }
        }

        var checkCommand = isWindows ? "where" : "which";
        var fileName = isWindows ? "ffmpeg.exe" : "ffmpeg";
        
        var searchName = Path.GetFileName(ffmpegPath);
        if (string.IsNullOrEmpty(searchName)) searchName = fileName;

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = checkCommand,
                Arguments = searchName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            string? foundPath = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(foundPath))
            {
                logger.LogInformation("ffmpeg found in PATH at {FfmpegInPath}.", foundPath.Trim().Split(Environment.NewLine).FirstOrDefault());
            }
            else
            {
                logger.LogError("ffmpeg is not found at the specified path {FfmpegPath} and is not available in PATH.", ffmpegPath);
                logger.LogError("Please ensure ffmpeg is installed and accessible via PATH or located in the application directory.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while checking for ffmpeg existence.");
        }
    }
}

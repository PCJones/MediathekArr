using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediathekArr.Utilities;

public static class FfmpegUtils
{
    public static async Task<(bool Success, int ExitCode, string ErrorOutput)> StartFfmpegDownloadAsync(
        string ffmpegPath, string url, string tsPath, string title, ILogger logger)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(url);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("copy");
        startInfo.ArgumentList.Add(tsPath);

        using var process = new Process { StartInfo = startInfo };

        try
        {
            logger.LogDebug("ffmpeg path: {FfmpegPath}", ffmpegPath);
            logger.LogDebug("Arguments: {Arguments}", string.Join(" ", startInfo.ArgumentList));

            process.Start();
            logger.LogInformation("FFmpeg download process started for {Title} with arguments: {Arguments}", title, string.Join(" ", startInfo.ArgumentList));

            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            var standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            string ffmpegOutput = await standardOutputTask;
            string ffmpegError = await standardErrorTask;

            logger.LogInformation("FFmpeg process completed for {Title}. Exit code: {ExitCode}", title, process.ExitCode);

            if (!string.IsNullOrWhiteSpace(ffmpegOutput))
            {
                logger.LogDebug("FFmpeg process standard output: {Output}", ffmpegOutput);
            }

            if (!string.IsNullOrWhiteSpace(ffmpegError))
            {
                logger.LogDebug("FFmpeg process error output: {Error}", ffmpegError);
            }

            if (process.ExitCode != 0)
            {
                logger.LogError("FFmpeg download failed for {Title}. Exit code: {ExitCode}.", title, process.ExitCode);
                return (false, process.ExitCode, string.IsNullOrWhiteSpace(ffmpegError) ? "Unknown error" : ffmpegError);
            }

            return (true, process.ExitCode, ffmpegError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the FFmpeg download for {Title}.", title);
            return (false, -1, ex.Message);
        }
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

        var startInfo = new ProcessStartInfo
        {
            FileName = checkCommand,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(searchName);

        using var process = new Process { StartInfo = startInfo };

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

using MediathekArr.Models;
using MediathekArr.Models.SABnzbd;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace MediathekArr.Services;

public partial class DownloadService
{
    private async Task DownloadM3u8ToMkvAsync(string url, string subtitleUrl, QueueItem queueItem, Stopwatch stopwatch)
    {
        var categoryDir = Path.Combine(_config.CompletePath, queueItem.Category);
        _logger.LogInformation("Ensuring directory exists for category {Category} at path: {Path}", queueItem.Category, categoryDir);
        Directory.CreateDirectory(categoryDir);

        var mkvPath = Path.Combine(categoryDir, queueItem.Title + ".mkv");

        queueItem.Status = DownloadStatus.Downloading;
        
        bool subtitlesAvailable = await DownloadSubtitlesAsync(subtitleUrl, queueItem);
        string subtitlePath = Path.Combine(_config.IncompletePath, queueItem.Title + ".srt");

        var subInput = "";
        var subMap = "";

        if (subtitlesAvailable && File.Exists(subtitlePath))
        {
            subInput = $"-i \"{subtitlePath}\" ";
            subMap = "-map 1:0 -c:s srt -metadata:s:s:0 language=ger ";
        }

        var ffmpegArgs = $"-i \"{url}\" {subInput}-map 0:v -map 0:a {subMap}-c:v copy -c:a copy -metadata:s:v:0 language=ger -metadata:s:a:0 language=ger \"{mkvPath}\"";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _isWindows ? "ffmpeg.exe" : "ffmpeg",
                Arguments = ffmpegArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            _logger.LogInformation("FFmpeg download process started for {Title} with arguments: {Arguments}", queueItem.Title, ffmpegArgs);

            var standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            string ffmpegOutput = await standardErrorTask;

            if (process.ExitCode == 0)
            {
                queueItem.Status = DownloadStatus.Completed;
                
                var fileInfo = new FileInfo(mkvPath);
                var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                queueItem.Size = $"{sizeInMB:F2} MB";
                queueItem.Sizeleft = "0 MB";
                queueItem.Percentage = "100";
                queueItem.Timeleft = "00:00:00";

                _logger.LogInformation("M3U8 Download completed successfully for {Title}. Output path: {MkvPath}", queueItem.Title, mkvPath);
            }
            else
            {
                queueItem.Status = DownloadStatus.Failed;
                _logger.LogError("FFmpeg M3U8 download failed for {Title}. Exit code: {ExitCode}. Error output: {ErrorOutput}", queueItem.Title, process.ExitCode, ffmpegOutput);
            }

            if (subtitlesAvailable && File.Exists(subtitlePath))
            {
                try
                {
                    File.Delete(subtitlePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting temporary files.");
                }
            }

            // Move completed (or failed) download to history
            var historyItem = new HistoryItem
            {
                Title = $"{queueItem.Title}.mkv",
                NzbName = queueItem.Title,
                Category = queueItem.Category,
                Size = File.Exists(mkvPath) ? new FileInfo(mkvPath).Length : 0,
                DownloadTime = (int)stopwatch.Elapsed.TotalSeconds,
                Storage = mkvPath,
                Status = queueItem.Status,
                Completed = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Id = queueItem.Id
            };
            _downloadHistory.Add(historyItem);

            _logger.LogInformation("Download history updated for {Title}. Status: {Status}, Download Time: {DownloadTime}s, Size: {Size} bytes",
                queueItem.Title, queueItem.Status, historyItem.DownloadTime, historyItem.Size);
        }
        catch (Exception ex)
        {
            queueItem.Status = DownloadStatus.Failed;
            _logger.LogError(ex, "An error occurred during the M3U8 download of {Title}.", queueItem.Title);
            
             _downloadHistory.Add(new HistoryItem
            {
                Title = $"{queueItem.Title}.mkv",
                NzbName = queueItem.Title,
                Category = queueItem.Category,
                Size = 0,
                DownloadTime = (int)stopwatch.Elapsed.TotalSeconds,
                Storage = null,
                Status = DownloadStatus.Failed,
                Completed = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Id = queueItem.Id
            });
        }
    }
}

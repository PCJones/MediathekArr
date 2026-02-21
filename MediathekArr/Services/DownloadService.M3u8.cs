using MediathekArr.Models;
using MediathekArr.Models.SABnzbd;
using MediathekArr.Utilities;

namespace MediathekArr.Services;

public partial class DownloadService
{
    private async Task DownloadM3u8FileAsync(string url, QueueItem queueItem)
    {
        try
        {
            var tsPath = Path.Combine(_config.IncompletePath, queueItem.Title + ".ts");

            _logger.LogInformation("Starting M3U8 download for {Title} to path: {Path}", queueItem.Title, tsPath);
            queueItem.Status = DownloadStatus.Downloading;

            var (success, exitCode, errorOutput) = await FfmpegUtils.StartFfmpegDownloadAsync(
                _ffmpegPath, url, tsPath, queueItem.Title, _logger);

            if (success && File.Exists(tsPath))
            {
                var fileInfo = new FileInfo(tsPath);
                var sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                queueItem.Size = $"{sizeInMB:F2}";

                queueItem.Timeleft = "00:00:00";
                _logger.LogInformation("M3U8 download completed for {Title}. File saved to {Path}", queueItem.Title, tsPath);
            }
            else
            {
                queueItem.Status = DownloadStatus.Failed;
                _logger.LogError("FFmpeg M3U8 download failed for {Title}. Exit code: {ExitCode}. Error: {ErrorOutput}",
                    queueItem.Title, exitCode, errorOutput);
            }
        }
        catch (Exception ex)
        {
            queueItem.Status = DownloadStatus.Failed;
            _logger.LogError(ex, "Download failed for {Title}. Adding to download history as failed.", queueItem.Title);

            _downloadHistory.Add(new HistoryItem
            {
                Title = queueItem.Title,
                NzbName = queueItem.Title,
                Category = queueItem.Category,
                Size = 0,
                DownloadTime = 0,
                Storage = null,
                Status = DownloadStatus.Failed,
                Completed = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Id = queueItem.Id
            });
        }
    }
}

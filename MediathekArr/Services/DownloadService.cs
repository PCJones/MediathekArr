using MediathekArrDownloader.Models;
using MediathekArrDownloader.Models.SABnzbd;
using MediathekArrDownloader.Utilities;
using MediathekArrLib.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MediathekArrDownloader.Services;

public partial class DownloadService
{
    private readonly ILogger<DownloadService> _logger;
    private readonly Config _config;
    private readonly ConcurrentQueue<SabnzbdQueueItem> _downloadQueue = new();
    private readonly List<SabnzbdHistoryItem> _downloadHistory = [];
    private readonly HttpClient _httpClient;
    private static readonly SemaphoreSlim _semaphore = new(2); // Limit concurrent downloads to 2
    private readonly string _mkvMergePath;
    private readonly bool _isWindows;

    public DownloadService(ILogger<DownloadService> logger, Config config, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config;
        _httpClient = httpClientFactory.CreateClient("MediathekClient");
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Set complete_dir based on the application's startup path
        var startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        _mkvMergePath = _isWindows ? Path.Combine(startupPath, "mkvtoolnix", "mkvmerge.exe") : "mkvmerge";

        InitializeIncompleteDirectory();
        InitializeCompleteDirectories();
        CleanupAbandondedFilesInCompleteDirectory();

        // Ensure Mkvmerge is available
        Task.Run(() => MkvMergeUtils.EnsureMkvMergeExistsAsync(_mkvMergePath, _logger, _httpClient)).Wait();
    }

    public void InitializeCompleteDirectories()
    {
        // Ensure complete directory exists
        if (!Directory.Exists(_config.CompletePath))
        {
            _logger.LogInformation("Complete directory doesn't exist yet, creating directory: {completeDir}", _config.CompletePath);
            Directory.CreateDirectory(_config.IncompletePath);
        }
        else
        {
            // Create category directories if they don't exist
            foreach (var category in _config.Categories)
            {
                var categoryDir = Path.Combine(_config.CompletePath, category);
                if (!Directory.Exists(categoryDir))
                {
                    _logger.LogInformation("Creating category directory: {categoryDir}", categoryDir);
                    Directory.CreateDirectory(categoryDir);
                }
            }
        }

    }

    private void CleanupAbandondedFilesInCompleteDirectory()
    {
        // Remove files older than 48 hours

        if (!Directory.Exists(_config.CompletePath))
        {
            return;
        }

        foreach (var category in _config.Categories)
        {
            var categoryDir = Path.Combine(_config.CompletePath, category);
            if (!Directory.Exists(categoryDir))
            {
                continue;
            }
            var files = Directory.GetFiles(categoryDir);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc > TimeSpan.FromHours(48))
                {
                    _logger.LogInformation("Deleting abandoned file in complete directory: {file}", file);
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting file: {file}", file);
                        continue;
                    }
                }
            }
        }
    }

    private void InitializeIncompleteDirectory()
    {
        // Ensure incomplete directory exists
        if (!Directory.Exists(_config.IncompletePath))
        {
            _logger.LogInformation("Incomplete directory doesn't exist yet, creating directory: {incompleteDir}", _config.IncompletePath);
            Directory.CreateDirectory(_config.IncompletePath);
        }
        else
        {
            // Delete everything inside the incomplete directory
            foreach (var file in Directory.GetFiles(_config.IncompletePath))
            {
                _logger.LogInformation("Deleting file in incomplete directory: {file}", file);
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting file: {file}", file);
                    continue;
                }
            }
        }
    }

    public IEnumerable<SabnzbdQueueItem> GetQueue() => [.. _downloadQueue];
    public IEnumerable<SabnzbdHistoryItem> GetHistory() => _downloadHistory;

    public SabnzbdQueueItem AddToQueue(string videoUrl, string subtitleUrl, string fileName, string category)
    {
        var queueItem = new SabnzbdQueueItem
        {
            Status = SabnzbdDownloadStatus.Queued,
            Index = _downloadQueue.Count,
            Timeleft = "10:00:00",
            Size = "0",
            Title = fileName,
            Category = category,
            Sizeleft = "0",
            Percentage = "0"
        };

        _downloadQueue.Enqueue(queueItem);

        Task.Run(() => StartDownloadAsync(videoUrl, subtitleUrl, queueItem));

        return queueItem;
    }

    private async Task StartDownloadAsync(string videoUrl, string subtitleUrl, SabnzbdQueueItem queueItem)
    {
        await _semaphore.WaitAsync();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting download for {Title} from URL: {URL}", queueItem.Title, videoUrl);

            var downloadVideoTask = DownloadFileAsync(videoUrl, queueItem);
            var downloadSubtitlesTask = DownloadSubtitlesAsync(subtitleUrl, queueItem);
            await Task.WhenAll(downloadVideoTask, downloadSubtitlesTask);
            var subtitlesAvailable = downloadSubtitlesTask.Result;

            if (queueItem.Status != SabnzbdDownloadStatus.Failed)
            {
                _logger.LogInformation("Download complete for {Title}. Starting conversion to MKV.", queueItem.Title);
                await ConvertMp4ToMkvAsync(queueItem, stopwatch, subtitlesAvailable);
            }
            else
            {
                _logger.LogWarning("Download failed for {Title}, skipping conversion.", queueItem.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during the download or conversion of {Title}.", queueItem.Title);
        }
        finally
        {
            _semaphore.Release();
            _downloadQueue.TryDequeue(out _);
            stopwatch.Stop();
        }
    }

    private async Task<bool> DownloadSubtitlesAsync(string subtitleUrl, SabnzbdQueueItem queueItem)
    {
        string? xmlFilePath = null;
        try
        {
            if (string.IsNullOrEmpty(subtitleUrl))
            {
                _logger.LogWarning("Subtitle URL is empty for {Title}. Skipping subtitle download.", queueItem.Title);
                return false;
            }

            xmlFilePath = Path.Combine(_config.IncompletePath, queueItem.Title + ".xml");
            var srtFilePath = Path.Combine(_config.IncompletePath, queueItem.Title + ".srt");

            // Download XML subtitle file
            _logger.LogInformation("Starting download of subtitle XML for {Title} to path: {Path}", queueItem.Title, xmlFilePath);
            var response = await _httpClient.GetAsync(subtitleUrl);
            response.EnsureSuccessStatusCode();

            await using (var contentStream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new FileStream(xmlFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await contentStream.CopyToAsync(fileStream);
            }

            _logger.LogInformation("Subtitle XML downloaded for {Title}. Converting to SRT format.", queueItem.Title);

            // Convert XML to SRT
            var xmlContent = await File.ReadAllTextAsync(xmlFilePath, Encoding.UTF8);
            var srtContent = SubtitleConverter.ConvertXmlToSrt(xmlContent);

            if (string.IsNullOrEmpty(srtContent))
            {
                return false;
            }

            // Save SRT file
            await File.WriteAllTextAsync(srtFilePath, srtContent, Encoding.UTF8);

            _logger.LogInformation("Subtitle conversion to SRT completed for {Title}. File saved to {Path}", queueItem.Title, srtFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subtitle download or conversion failed for {Title}.", queueItem.Title);
        }
        finally
        {
            if (!string.IsNullOrEmpty(xmlFilePath) && File.Exists(xmlFilePath))
            {
                try
                {
                    File.Delete(xmlFilePath);
                    _logger.LogInformation("Temporary subtitle XML file deleted: {Path}", xmlFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete temporary subtitle XML file: {Path}", xmlFilePath);
                }
            }
        }

        return false;
    }

    private async Task DownloadFileAsync(string url, SabnzbdQueueItem queueItem)
    {
        try
        {
            var fileExtension = Path.GetExtension(url) ?? ".mp4";
            var filePath = Path.Combine(_config.IncompletePath, queueItem.Title + fileExtension);

            if (File.Exists(filePath))
            {
                _logger.LogWarning("Removing existing file in temp directory: {filePath}", filePath);
            }

            _logger.LogInformation("Starting download of file to path: {Path} with extension {Extension}", filePath, fileExtension);
            queueItem.Status = SabnzbdDownloadStatus.Downloading;

            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            var totalSize = response.Content.Headers.ContentLength ?? 0;

            queueItem.Size = (totalSize / (1024.0 * 1024.0)).ToString("F2");
            _logger.LogInformation("Total file size for {Title}: {Size} MB", queueItem.Title, queueItem.Size);

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = new byte[8192];
                var totalRead = 0L;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    totalRead += bytesRead;

                    // Update queue item progress
                    queueItem.Sizeleft = ((totalSize - totalRead) / (1024.0 * 1024.0)).ToString("F2");
                    queueItem.Percentage = (totalRead / (double)totalSize * 100).ToString("F0");

                    _logger.LogDebug("Download progress for {Title}: {Percentage}% - {SizeLeft} MB remaining", queueItem.Title, queueItem.Percentage, queueItem.Sizeleft);
                }
            }

            queueItem.Timeleft = "00:00:00";
            _logger.LogInformation("Download completed for {Title}. File saved to {Path}", queueItem.Title, filePath);
        }
        catch (Exception ex)
        {
            queueItem.Status = SabnzbdDownloadStatus.Failed;
            _logger.LogError(ex, "Download failed for {Title}. Adding to download history as failed.", queueItem.Title);

            _downloadHistory.Add(new SabnzbdHistoryItem
            {
                Title = queueItem.Title,
                NzbName = queueItem.Title,
                Category = queueItem.Category,
                Size = 0,
                DownloadTime = 0,
                Storage = null,
                Status = SabnzbdDownloadStatus.Failed,
                Completed = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Id = queueItem.Id
            });
        }
    }
    public bool DeleteHistoryItem(string nzoId, bool delFiles)
    {
        var item = _downloadHistory.FirstOrDefault(h => h.Id == nzoId);

        if (item == null)
        {
            return false;
        }

        // Optionally delete the associated file
        if (delFiles && !string.IsNullOrEmpty(item.Storage) && File.Exists(item.Storage))
        {
            try
            {
                File.Delete(item.Storage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
            }
        }

        // Remove the item from the history list
        _downloadHistory.Remove(item);
        return true;
    }

    private async Task ConvertMp4ToMkvAsync(SabnzbdQueueItem queueItem, Stopwatch stopwatch, bool subtitlesAvailable)
    {
        var completeCategoryDir = Path.Combine(_config.CompletePath, queueItem.Category);
        _logger.LogInformation("Ensuring complete directory exists at path: {Path}", completeCategoryDir);
        Directory.CreateDirectory(completeCategoryDir);

        var mp4Path = Path.Combine(_config.IncompletePath, queueItem.Title + ".mp4");
        var subtitlePath = Path.Combine(_config.IncompletePath, queueItem.Title + ".srt");
        var mkvPath = Path.Combine(completeCategoryDir, queueItem.Title + ".mkv");

        if (!File.Exists(mp4Path))
        {
            queueItem.Status = SabnzbdDownloadStatus.Failed;
            _logger.LogWarning("MP4 file not found for conversion. Path: {Mp4Path}. Marking as failed.", mp4Path);
            return;
        }

        if (subtitlesAvailable && !File.Exists(subtitlePath))
        {
            _logger.LogError("Subtitle file not found for conversion. Path: {SubtitlePath}. Continuing without subtitles.", subtitlePath);
            subtitlesAvailable = false;
        }

        // Temporarily remove umlauts as mkvmerge can't handle them on linux
        var mp4PathWithoutUmlauts = mp4Path.RemoveUmlauts();
        var subtitlePathWithoutUmlauts = subtitlePath.RemoveUmlauts();
        var mkvPathWithoutUmlauts = mkvPath.RemoveUmlauts();
        if (mp4PathWithoutUmlauts != mp4Path)
        {
            File.Move(mp4Path, mp4PathWithoutUmlauts);
        }
        if (subtitlesAvailable && subtitlePathWithoutUmlauts != subtitlePath)
        {
            File.Move(subtitlePath, subtitlePathWithoutUmlauts);
        }

        queueItem.Status = SabnzbdDownloadStatus.Extracting;
        _logger.LogInformation("Starting conversion of {Title} from MP4 to MKV. MP4 Path: {Mp4Path}, MKV Path: {MkvPath}", queueItem.Title, mp4Path, mkvPathWithoutUmlauts);

        var (success, exitCode, errorOutput) = await MkvMergeUtils.StartMkvmergeProcessAsync(_mkvMergePath, mp4PathWithoutUmlauts, subtitlePathWithoutUmlauts, mkvPathWithoutUmlauts, subtitlesAvailable, queueItem.Title, _logger);

        // Restore umlauts so *arrs correctly identify the show
        if (mkvPathWithoutUmlauts != mkvPath)
        {
            File.Move(mkvPathWithoutUmlauts, mkvPath);
        }

        if (success)
        {
            queueItem.Status = SabnzbdDownloadStatus.Completed;
            _logger.LogInformation("Conversion completed successfully for {Title}. Output path: {MkvPath}", queueItem.Title, mkvPath);
        }
        else
        {
            queueItem.Status = SabnzbdDownloadStatus.Failed;
            _logger.LogError("Mkvmerge conversion failed for {Title}. Exit code: {ExitCode}. Error output: {ErrorOutput}", queueItem.Title, exitCode, errorOutput);
        }

        DeleteTemporaryFiles(mp4PathWithoutUmlauts, subtitlePathWithoutUmlauts, subtitlesAvailable);

        double sizeInMB = 0;
        if (double.TryParse(queueItem.Size.Replace("GB", "").Replace("MB", "").Trim(), out double size))
        {
            sizeInMB = queueItem.Size.Contains("GB") ? size * 1024 : size;
        }

        // Move completed download to history
        var historyItem = new SabnzbdHistoryItem
        {
            Title = $"{queueItem.Title}.mkv",
            NzbName = queueItem.Title,
            Category = queueItem.Category,
            Size = (long)(sizeInMB * 1024 * 1024), // Convert MB to bytes
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

    public void DeleteTemporaryFiles(string mp4Path, string subtitlePath, bool subtitlesAvailable)
    {
        try
        {
            File.Delete(mp4Path);
            if (subtitlesAvailable)
            {
                File.Delete(subtitlePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting temporary files.");
        }
    }
}

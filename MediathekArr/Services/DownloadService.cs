﻿using MediathekArrDownloader.Models;
using MediathekArrDownloader.Models.SABnzbd;
using MediathekArrDownloader.Utilities;
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
    private static readonly HttpClient _httpClient = new();
    private static readonly SemaphoreSlim _semaphore = new(2); // Limit concurrent downloads to 2
    private readonly string _ffmpegPath;
    private readonly bool _isWindows;

    public DownloadService(ILogger<DownloadService> logger, Config config)
    {
        _logger = logger;
        _config = config;
        _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Set complete_dir based on the application's startup path
        var startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
        _ffmpegPath = Path.Combine(startupPath, "ffmpeg", _isWindows ? "ffmpeg.exe" : "ffmpeg");

        InitializeIncompleteDirectory();
        CleanupAbandondedFilesInCompleteDirectory();

        // Ensure FFmpeg is available
        Task.Run(() => FfmpegUtils.EnsureFfmpegExistsAsync(_ffmpegPath, _isWindows, _logger, _httpClient)).Wait();
    }

    private void CleanupAbandondedFilesInCompleteDirectory()
    {
        // Remove files older than 48 hours

        if (!Directory.Exists(_config.CompletePath))
        {
            return;
        }

        var files = Directory.GetFiles(_config.CompletePath);
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

    private void InitializeIncompleteDirectory()
    {
        // Ensure incomplete directory exists
        if (!Directory.Exists(_config.IncompletePath))
        {
            _logger.LogInformation("Ensuring incomplete doesn't exist, creating directory: {incompleteDir}", _config.IncompletePath);
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
            var response = await _httpClient.GetAsync(subtitleUrl, HttpCompletionOption.ResponseHeadersRead);
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
        var completeCategoryDir = _config.CompletePath;
        _logger.LogInformation("Ensuring directory exists for category {Category} at path: {Path}", queueItem.Category, completeCategoryDir);
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

        queueItem.Status = SabnzbdDownloadStatus.Extracting;
        _logger.LogInformation("Starting conversion of {Title} from MP4 to MKV. MP4 Path: {Mp4Path}, MKV Path: {MkvPath}", queueItem.Title, mp4Path, mkvPath);

        var (success, exitCode, errorOutput) = await FfmpegUtils.StartFfmpegProcessAsync(_ffmpegPath, mp4Path, subtitlePath, mkvPath, subtitlesAvailable, queueItem.Title, _logger);

        if (success)
        {
            queueItem.Status = SabnzbdDownloadStatus.Completed;
            _logger.LogInformation("Conversion completed successfully for {Title}. Output path: {MkvPath}", queueItem.Title, mkvPath);
        }
        else
        {
            queueItem.Status = SabnzbdDownloadStatus.Failed;
            _logger.LogError("FFmpeg conversion failed for {Title}. Exit code: {ExitCode}. Error output: {ErrorOutput}", queueItem.Title, exitCode, errorOutput);
        }

        DeleteTemporaryFiles(mp4Path, subtitlePath, subtitlesAvailable);

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

    private async Task EnsureFfmpegExistsAsync()
    {
        if (!File.Exists(_ffmpegPath))
        {
            _logger.LogInformation("FFmpeg not found at path {FfmpegPath}. Starting download...", _ffmpegPath);

            // URLs for downloading FFmpeg based on OS
            string ffmpegDownloadUrl = _isWindows
                ? "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
                : "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz";

            var tempFilePath = Path.Combine(Path.GetTempPath(), _isWindows ? "ffmpeg.zip" : "ffmpeg.tar.xz");
            var ffmpegDir = Path.Combine(Path.GetDirectoryName(_ffmpegPath) ?? string.Empty);

            try
            {
                // Download FFmpeg file
                using (var response = await _httpClient.GetAsync(ffmpegDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                    _logger.LogInformation("FFmpeg downloaded to temporary path {TempFilePath}", tempFilePath);
                }

                Directory.CreateDirectory(ffmpegDir);
                _logger.LogInformation("FFmpeg directory ensured at {FfmpegDir}", ffmpegDir);

                // Extract FFmpeg based on the OS
                if (_isWindows)
                {
                    ZipFile.ExtractToDirectory(tempFilePath, ffmpegDir);
                    _logger.LogInformation("FFmpeg extracted in Windows environment.");

                    // Move extracted ffmpeg.exe to the expected path
                    var extractedPath = Directory.GetFiles(ffmpegDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (extractedPath != null)
                    {
                        File.Move(extractedPath, _ffmpegPath, true);
                        _logger.LogInformation("FFmpeg moved to final path {FfmpegPath}", _ffmpegPath);
                    }
                }
                else
                {
                    // Linux/macOS extraction
                    var extractionDir = Path.Combine(ffmpegDir, "extracted");
                    Directory.CreateDirectory(extractionDir);

                    _logger.LogInformation("Starting extraction of FFmpeg in Linux environment.");

                    var tarProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "tar",
                            Arguments = $"-xf \"{tempFilePath}\" -C \"{extractionDir}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    tarProcess.Start();
                    await tarProcess.WaitForExitAsync();

                    if (tarProcess.ExitCode != 0)
                    {
                        string error = await tarProcess.StandardError.ReadToEndAsync();
                        _logger.LogError("Error extracting FFmpeg: {Error}", error);
                        return;
                    }

                    _logger.LogInformation("FFmpeg extraction completed.");

                    // Locate the extracted FFmpeg binary
                    var extractedPath = Directory.GetFiles(extractionDir, "ffmpeg", SearchOption.AllDirectories).FirstOrDefault();
                    if (extractedPath != null)
                    {
                        File.Move(extractedPath, _ffmpegPath, true);
                        _logger.LogInformation("FFmpeg moved to final path {FfmpegPath}", _ffmpegPath);

                        // Ensure the binary is executable
                        var chmodProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "chmod",
                                Arguments = $"+x \"{_ffmpegPath}\"",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        chmodProcess.Start();
                        await chmodProcess.WaitForExitAsync();
                        _logger.LogInformation("Executable permissions set for FFmpeg at {FfmpegPath}", _ffmpegPath);
                    }
                    else
                    {
                        _logger.LogError("FFmpeg binary not found after extraction.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during FFmpeg download or extraction.");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    _logger.LogInformation("Temporary download file deleted at {TempFilePath}", tempFilePath);
                }

                var extractionDir = Path.Combine(ffmpegDir, "extracted");
                if (Directory.Exists(extractionDir))
                {
                    Directory.Delete(extractionDir, true);
                    _logger.LogInformation("Temporary extraction directory deleted at {ExtractionDir}", extractionDir);
                }
            }

            _logger.LogInformation("FFmpeg download and setup complete.");
        }
        else
        {
            _logger.LogInformation("FFmpeg already exists at path {FfmpegPath}. Skipping download.", _ffmpegPath);
        }
    }
}

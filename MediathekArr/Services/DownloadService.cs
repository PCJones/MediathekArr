using MediathekArr.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediathekArr.Services
{
    public partial class DownloadService
    {
        private readonly ConcurrentQueue<SabnzbdQueueItem> _downloadQueue = new();
        private readonly List<SabnzbdHistoryItem> _downloadHistory = new();
        private static readonly HttpClient _httpClient = new();
        private static readonly SemaphoreSlim _semaphore = new(2); // Limit concurrent downloads to 2
        private readonly string _completeDir;
        private readonly string _ffmpegPath;
        private readonly bool _isWindows;

        public DownloadService()
        {
            _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            // Set complete_dir based on the application's startup path
            var startupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            _completeDir = Path.Combine(startupPath, "downloads");
            _ffmpegPath = Path.Combine(startupPath, "ffmpeg", _isWindows ? "ffmpeg.exe" : "ffmpeg");

            ClearDownloadsDirectory();

            // Ensure FFmpeg is available
            Task.Run(EnsureFfmpegExistsAsync).Wait();
        }

        private void ClearDownloadsDirectory()
        {
            if (Directory.Exists(_completeDir))
            {
                try
                {
                    Console.WriteLine($"Clearing downloads directory");

                    // Delete all files in the directory
                    foreach (var file in Directory.GetFiles(_completeDir))
                    {
                        File.Delete(file);
                    }

                    // Delete all subdirectories and their contents
                    foreach (var directory in Directory.GetDirectories(_completeDir))
                    {
                        Directory.Delete(directory, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error clearing downloads directory: {ex.Message}");
                }
            }
        }


        public IEnumerable<SabnzbdQueueItem> GetQueue() => [.. _downloadQueue];
        public IEnumerable<SabnzbdHistoryItem> GetHistory() => _downloadHistory;

        public SabnzbdQueueItem AddToQueue(string url, string fileName, string category)
        {
            var queueItem = new SabnzbdQueueItem
            {
                Status = SabnzbdDownloadStatus.Queued,
                Index = _downloadQueue.Count,
                Timeleft = "10:00:00",
                Size = "Unknown",
                Title = fileName,
                Category = category,
                Sizeleft = "Unknown",
                Percentage = "0"
            };

            _downloadQueue.Enqueue(queueItem);

            Task.Run(() => StartDownloadAsync(url, queueItem));

            return queueItem;
        }

        private async Task StartDownloadAsync(string url, SabnzbdQueueItem queueItem)
        {
            await _semaphore.WaitAsync();
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await DownloadFileAsync(url, queueItem, stopwatch);
                if (queueItem.Status != SabnzbdDownloadStatus.Failed)
                {
                    await ConvertMp4ToMkvAsync(queueItem, stopwatch);
                }
            }
            finally
            {
                _semaphore.Release();
                _downloadQueue.TryDequeue(out _);
                stopwatch.Stop();
            }
        }
        private async Task DownloadFileAsync(string url, SabnzbdQueueItem queueItem, Stopwatch stopwatch)
        {
            try
            {
                var categoryDir = Path.Combine(_completeDir, queueItem.Category);
                Directory.CreateDirectory(categoryDir);

                var fileExtension = Path.GetExtension(url) ?? ".mp4";
                var filePath = Path.Combine(categoryDir, queueItem.Title + fileExtension);

                queueItem.Status = SabnzbdDownloadStatus.Downloading;

                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                var totalSize = response.Content.Headers.ContentLength ?? 0;

                queueItem.Size = (totalSize / (1024.0 * 1024.0)).ToString("F2");

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
                    }
                }

                queueItem.Timeleft = "00:00:00";
            }
            catch
            {
                queueItem.Status = SabnzbdDownloadStatus.Failed;

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

        private async Task ConvertMp4ToMkvAsync(SabnzbdQueueItem queueItem, Stopwatch stopwatch)
        {
            var categoryDir = Path.Combine(_completeDir, queueItem.Category);
            var mp4Path = Path.Combine(categoryDir, queueItem.Title + ".mp4");
            var mkvPath = Path.Combine(categoryDir, queueItem.Title + ".mkv");

            if (!File.Exists(mp4Path))
            {
                queueItem.Status = SabnzbdDownloadStatus.Failed;
                return;
            }

            queueItem.Status = SabnzbdDownloadStatus.Extracting;

            var ffmpegArgs = $"-i \"{mp4Path}\" -map 0:v -map 0:a -c copy -metadata:s:v:0 language=ger -metadata:s:a:0 language=ger \"{mkvPath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var standardErrorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string ffmpegOutput = await standardErrorTask;

            if (process.ExitCode == 0)
            {
                queueItem.Status = SabnzbdDownloadStatus.Completed;
            }
            else
            {
                queueItem.Status = SabnzbdDownloadStatus.Failed;
                Console.WriteLine("FFmpeg Error Output:");
                Console.WriteLine(ffmpegOutput);
            }

            double sizeInMB = 0;
            if (double.TryParse(queueItem.Size.Replace("GB", "").Replace("MB", "").Trim(), out double size))
            {
                sizeInMB = queueItem.Size.Contains("GB") ? size * 1024 : size;
            }

            // Move completed download to history
            _downloadHistory.Add(new SabnzbdHistoryItem
            {
                Title = queueItem.Title,
                NzbName = queueItem.Title,
                Category = queueItem.Category,
                Size = (long)(sizeInMB * 1024 * 1024), // Convert MB to bytes
                DownloadTime = (int)stopwatch.Elapsed.TotalSeconds,
                Storage = mkvPath,
                Status = queueItem.Status,
                Id = queueItem.Id
            });
        }

        private async Task EnsureFfmpegExistsAsync()
        {
            if (!File.Exists(_ffmpegPath))
            {
                Console.WriteLine("FFmpeg not found. Downloading...");

                // URLs for downloading FFmpeg based on OS
                string ffmpegDownloadUrl = _isWindows
                    ? "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
                    : "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz";

                var tempFilePath = Path.Combine(Path.GetTempPath(), _isWindows ? "ffmpeg.zip" : "ffmpeg.tar.xz");

                using (var response = await _httpClient.GetAsync(ffmpegDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                var ffmpegDir = Path.Combine(Path.GetDirectoryName(_ffmpegPath) ?? string.Empty);
                Directory.CreateDirectory(ffmpegDir);

                // Extract FFmpeg based on the OS
                if (_isWindows)
                {
                    ZipFile.ExtractToDirectory(tempFilePath, ffmpegDir);
                    File.Delete(tempFilePath);

                    // Move extracted ffmpeg.exe to the expected path
                    var extractedPath = Directory.GetFiles(ffmpegDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (extractedPath != null)
                    {
                        File.Move(extractedPath, _ffmpegPath, true);
                    }
                }
                else
                {
                    // Linux/macOS extraction
                    var extractionDir = Path.Combine(ffmpegDir, "extracted");
                    Directory.CreateDirectory(extractionDir);

                    // Use tar command to extract
                    var process = new Process
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
                    process.Start();
                    await process.WaitForExitAsync();

                    // Move extracted ffmpeg to the expected path
                    var extractedPath = Directory.GetFiles(extractionDir, "ffmpeg", SearchOption.AllDirectories).FirstOrDefault();
                    if (extractedPath != null)
                    {
                        File.Move(extractedPath, _ffmpegPath, true);
                    }

                    File.Delete(tempFilePath);
                    Directory.Delete(extractionDir, true);
                }

                Console.WriteLine("FFmpeg downloaded and extracted.");
            }
        }
    }
}

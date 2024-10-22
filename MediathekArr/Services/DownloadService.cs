using MediathekArr.Models;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MediathekArr.Services
{
    public class DownloadService
    {
        private readonly ConcurrentQueue<SabnzbdQueueItem> _downloadQueue = new();
        private readonly List<SabnzbdHistoryItem> _downloadHistory = new();
        private static readonly HttpClient _httpClient = new();

        public IEnumerable<SabnzbdQueueItem> GetQueue() => _downloadQueue;

        public IEnumerable<SabnzbdHistoryItem> GetHistory() => _downloadHistory;

        public void AddToQueue(string url, string category)
        {
            var queueItem = new SabnzbdQueueItem
            {
                Status = "Queued",
                Index = _downloadQueue.Count,
                Timeleft = "Unknown",
                Size = "Unknown",
                Title = Path.GetFileName(url),
                Category = category,
                Sizeleft = "Unknown",
                Percentage = "0",
                Id = System.Guid.NewGuid().ToString()
            };

            _downloadQueue.Enqueue(queueItem);

            // Start download asynchronously
            Task.Run(() => DownloadFileAsync(url, queueItem));
        }

        private async Task DownloadFileAsync(string url, SabnzbdQueueItem queueItem)
        {
            try
            {
                queueItem.Status = "Downloading";

                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                var totalSize = response.Content.Headers.ContentLength ?? 0;

                var filePath = Path.Combine("C:\\Users\\Admin\\Downloads\\", queueItem.Title);
                queueItem.Size = (totalSize / (1024.0 * 1024.0)).ToString("F2");

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    var totalRead = 0L;
                    var bytesRead = 0;

                    while ((bytesRead = await response.Content.ReadAsByteArrayAsync()) != 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                        totalRead += bytesRead;

                        queueItem.Sizeleft = ((totalSize - totalRead) / (1024.0 * 1024.0)).ToString("F2");
                        queueItem.Percentage = ((totalRead / (double)totalSize) * 100).ToString("F0");
                    }
                }

                queueItem.Status = "Completed";
                queueItem.Timeleft = "00:00:00";

                _downloadHistory.Add(new SabnzbdHistoryItem
                {
                    Title = queueItem.Title,
                    NzbName = queueItem.Title,
                    Category = queueItem.Category,
                    Size = totalSize,
                    DownloadTime = 0, // Placeholder, you can calculate this
                    Storage = filePath,
                    Status = SabnzbdDownloadStatus.Completed,
                    Id = queueItem.Id
                });
            }
            catch
            {
                queueItem.Status = "Failed";
            }
        }
    }
}

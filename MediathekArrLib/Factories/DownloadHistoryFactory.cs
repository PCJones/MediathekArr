using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediathekArr.Models.SABnzbd;

namespace MediathekArr.Factories;

public static class DownloadHistoryFactory
{
    public static HistoryItem ToFailedItem(this QueueItem queueItem)
        => new()
        {
            Title = queueItem.Title,
            NzbName = queueItem.Title,
            Category = queueItem.Category,
            Size = 0,
            DownloadTime = 0,
            Storage = null,
            Status = DownloadStatus.Failed,
            Id = queueItem.Id
        };

    public static HistoryItem ToHistoryItem(this QueueItem queueItem, string path, double size, double timeElapsed)
        => new()
        {
            Title = $"{queueItem.Title}.mkv",
            NzbName = queueItem.Title,
            Category = queueItem.Category,
            Size = (long)(size * 1024 * 1024), // Convert MB to bytes
            DownloadTime = (int)timeElapsed,
            Storage = path,
            Status = queueItem.Status,
            Id = queueItem.Id
        };
}

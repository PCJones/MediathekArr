using System.Text.Json.Serialization;

namespace MediathekArrDownloader.Models.SABnzbd;

public class SabnzbdQueue
{
    [JsonPropertyName("paused")]
    public bool Paused => false;

    [JsonPropertyName("slots")]
    public List<SabnzbdQueueItem> Items { get; set; }
}
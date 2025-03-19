using System.Text.Json.Serialization;

namespace MediathekArr.Models.SABnzbd;

public class Queue
{
    [JsonPropertyName("paused")]
    public bool Paused => false;

    [JsonPropertyName("slots")]
    public List<QueueItem> Items { get; set; }
}
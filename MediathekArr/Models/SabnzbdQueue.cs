using System.Text.Json.Serialization;

namespace MediathekArr.Models;

public class SabnzbdQueue
{
    [JsonPropertyName("paused")]
    public bool Paused => false;

    [JsonPropertyName("slots")]
    public List<SabnzbdQueueItem> Items { get; set; }
}
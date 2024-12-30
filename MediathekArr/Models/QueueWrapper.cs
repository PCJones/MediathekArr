using System.Text.Json.Serialization;

namespace MediathekArrDownloader.Models;

public class QueueWrapper
{
    [JsonPropertyName("queue")]
    public SabnzbdQueue Queue { get; set; }
}

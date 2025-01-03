using System.Text.Json.Serialization;

namespace MediathekArrDownloader.Models.SABnzbd;

public class QueueWrapper
{
    [JsonPropertyName("queue")]
    public SabnzbdQueue Queue { get; set; }
}

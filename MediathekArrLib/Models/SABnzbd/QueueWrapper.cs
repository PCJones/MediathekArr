using System.Text.Json.Serialization;

namespace MediathekArr.Models.SABnzbd;

public class QueueWrapper
{
    [JsonPropertyName("queue")]
    public Queue Queue { get; set; }
}

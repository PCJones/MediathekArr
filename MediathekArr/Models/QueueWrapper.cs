using System.Text.Json.Serialization;

namespace MediathekArr.Models;

public class QueueWrapper
{
    [JsonPropertyName("queue")]
    public SabnzbdQueue Queue { get; set; }
}

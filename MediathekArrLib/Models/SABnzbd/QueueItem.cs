using System.Text.Json.Serialization;

namespace MediathekArr.Models.SABnzbd;

public class QueueItem
{
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DownloadStatus Status { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("timeleft")]
    public string Timeleft { get; set; } // "0:00:00"

    [JsonPropertyName("mb")]
    public string Size { get; set; } // "1163.54"

    [JsonPropertyName("filename")]
    public string Title { get; set; }

    [JsonPropertyName("priority")]
    public string Priority => "Normal";

    [JsonPropertyName("cat")]
    public string Category { get; set; }

    [JsonPropertyName("mbleft")]
    public string Sizeleft { get; set; } // "756.4 MB"

    [JsonPropertyName("percentage")]
    public string Percentage { get; set; } // "34"

    [JsonPropertyName("nzo_id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
}

using System.Text.Json.Serialization;

namespace MediathekArrDownloader.Models.SABnzbd;

public class SabnzbdHistoryItem
{
    [JsonPropertyName("fail_message")]
    public string FailMessage { get; set; }

    [JsonPropertyName("bytes")]
    public long Size { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("nzb_name")]
    public string NzbName { get; set; }

    [JsonPropertyName("download_time")]
    public int DownloadTime { get; set; }

    [JsonPropertyName("storage")]
    public string Storage { get; set; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SabnzbdDownloadStatus Status { get; set; }

    [JsonPropertyName("completed")]
    public long Completed { get; set; }

    [JsonPropertyName("nzo_id")]
    public string Id { get; set; }

    [JsonPropertyName("postproc_time")]
    public int PostprocTime => 0;

    [JsonPropertyName("name")]
    public string Title { get; set; }
}

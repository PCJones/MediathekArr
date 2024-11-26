using System.Text.Json.Serialization;

namespace MediathekArr.Models;

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

    [JsonPropertyName("nzo_id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Title { get; set; }
}

public class HistoryWrapper
{
    [JsonPropertyName("history")]
    public SabnzbdHistory History { get; set; }
}

public class SabnzbdHistory
{
    [JsonPropertyName("slots")]
    public List<SabnzbdHistoryItem> Items { get; set; }
}

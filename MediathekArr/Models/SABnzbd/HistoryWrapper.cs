using System.Text.Json.Serialization;

namespace MediathekArrDownloader.Models.SABnzbd;

public class HistoryWrapper
{
    [JsonPropertyName("history")]
    public SabnzbdHistory History { get; set; }
}

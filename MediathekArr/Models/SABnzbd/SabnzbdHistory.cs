using System.Text.Json.Serialization;

namespace MediathekArrDownloader.Models.SABnzbd;

public class SabnzbdHistory
{
    [JsonPropertyName("slots")]
    public List<SabnzbdHistoryItem> Items { get; set; }
}

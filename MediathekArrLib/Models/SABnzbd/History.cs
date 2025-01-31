using System.Text.Json.Serialization;

namespace MediathekArr.Models.SABnzbd;

public class History
{
    [JsonPropertyName("slots")]
    public List<HistoryItem> Items { get; set; }
}

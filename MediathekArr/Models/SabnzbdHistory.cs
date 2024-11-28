using System.Text.Json.Serialization;

namespace MediathekArr.Models;

public class SabnzbdHistory
{
    [JsonPropertyName("slots")]
    public List<SabnzbdHistoryItem> Items { get; set; }
}

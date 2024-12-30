using System.Text.Json.Serialization;

namespace MediathekArrDownloader.Models;

public class HistoryWrapper
{
    [JsonPropertyName("history")]
    public SabnzbdHistory History { get; set; }
}

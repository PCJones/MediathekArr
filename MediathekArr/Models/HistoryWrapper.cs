using System.Text.Json.Serialization;

namespace MediathekArr.Models;

public class HistoryWrapper
{
    [JsonPropertyName("history")]
    public SabnzbdHistory History { get; set; }
}

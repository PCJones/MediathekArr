using System.Text.Json.Serialization;

namespace MediathekArr.Models.SABnzbd;

public class HistoryWrapper
{
    [JsonPropertyName("history")]
    public History History { get; set; }
}

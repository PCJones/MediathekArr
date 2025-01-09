using System.Text.Json.Serialization;

namespace MediathekArr.Models;

public class QueryInfo
{
    [JsonPropertyName("filmlisteTimestamp")]
    public long FilmlisteTimestamp { get; set; }

    [JsonPropertyName("searchEngineTime")]
    public string SearchEngineTime { get; set; }

    [JsonPropertyName("resultCount")]
    public int ResultCount { get; set; }

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }
}
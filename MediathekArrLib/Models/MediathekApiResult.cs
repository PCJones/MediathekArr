using System.Text.Json.Serialization;

namespace MediathekArrLib.Models;

public class MediathekApiResult
{
    [JsonPropertyName("results")]
    public List<ApiResultItem> Results { get; set; }

    [JsonPropertyName("queryInfo")]
    public QueryInfo QueryInfo { get; set; }
}

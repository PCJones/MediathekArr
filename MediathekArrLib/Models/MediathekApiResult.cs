using System.Text.Json.Serialization;

namespace MediathekArr.Models;

public class MediathekApiResult
{
    [JsonPropertyName("results")]
    public List<ApiResultItem> Results { get; set; }

    [JsonPropertyName("queryInfo")]
    public QueryInfo QueryInfo { get; set; }
}

using System.Text.Json.Serialization;

namespace MediathekArrLib.Models.Rulesets
{
    public class RulesetApiResponse
    {
        [JsonPropertyName("rulesets")]
        public List<Ruleset> Rulesets { get; set; } = new();

        [JsonPropertyName("pagination")]
        public Pagination Pagination { get; set; } = new();
    }

    public class Pagination
    {
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }

        [JsonPropertyName("itemsPerPage")]
        public int ItemsPerPage { get; set; }
    }
}

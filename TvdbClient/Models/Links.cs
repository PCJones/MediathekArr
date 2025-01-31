using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tvdb.Models;

/// <summary>
/// Links for next, previous and current record
/// </summary>
public class Links
{

    [System.Text.Json.Serialization.JsonPropertyName("prev")]
    public string Prev { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("self")]
    public string Self { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("next")]
    public string Next { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("total_items")]
    public int? Total_items { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("page_size")]
    public int? Page_size { get; set; }

    private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

    [System.Text.Json.Serialization.JsonExtensionData]
    public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
        set { _additionalProperties = value; }
    }

}
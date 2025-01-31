using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tvdb.Models;

/// <summary>
/// Wrapper for TVDB API Responses
/// </summary>
/// <typeparam name="TDataType"></typeparam>
public class ApiResponseWrapper<TDataType> 
    where TDataType : class
{
    /// <summary>
    /// Request Status
    /// </summary>
    /// <remarks>Can be success, failure</remarks>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Is Response a success?
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsSuccess => Status.Equals("success");

    /// <summary>
    /// Error Message in case of a Failure
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string? ErrorMessage { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public TDataType? Data { get; set; }

    private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

    [System.Text.Json.Serialization.JsonExtensionData]
    public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
    {
        get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
        set { _additionalProperties = value; }
    }

    [System.Text.Json.Serialization.JsonPropertyName("links")]
    public Links Links { get; set; }
}

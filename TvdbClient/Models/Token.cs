using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tvdb.Extensions;

namespace Tvdb.Models;

public class Token
{
    [System.Text.Json.Serialization.JsonPropertyName("token")]
    public string AccessToken { get; set; }

    public DateTime CreationTimestamp { get; } = DateTime.Now;

    /// <summary>
    /// Expiry Date
    /// </summary>
    /// <remarks>TVDB says their tokens last a month</remarks>
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime TokenExpiryDate => CreationTimestamp.AddDays(30);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsTokenExpired => TokenExpiryDate.IsInThePast();

    /// <summary>
    /// Token Type
    /// </summary>
    /// <remarks>Hardcoded Bearer even though its not quite a Bearer Token but ok</remarks>
    public string TokenType => "Bearer";
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediathekArr.Configuration;

public class MediathekArrApiConfiguration
{
    /// <summary>
    /// Api Key
    /// </summary>
    public virtual string ApiKey { get; set; }

    /// <summary>
    /// Base URL for the API
    /// </summary>
    public virtual string BaseUrl { get; set; }
}
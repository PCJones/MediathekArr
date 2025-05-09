﻿using System.Text.Json.Serialization;

namespace MediathekArrLib.Models;

public class MediathekApiResponse
{
    [JsonPropertyName("result")]
    public MediathekApiResult Result { get; set; }

    [JsonPropertyName("err")]
    public object? Err { get; set; }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediathekArr.Constants;

public static class MediathekArrConstants
{
    public const string Mediathek_HttpClient = "MediathekClient";
    public const string MediathekArr_Api_Base_Url = "https://mediathekarr.pcjones.de/api/v1";
    
    /* Caching (int in Hours) */
    public const int MediathekArr_MemoryCache_Expiry = 12;
    public const int MediathekArr_DatabaseCache_Expiry = 24;
}

public static class EnvironmentVariableConstants
{
    public const string Api_Base_Url = "MEDIATHEKARR_API_BASE_URL";
    public const string Config_Path = "CONFIG_PATH";
    public const string Download_Path_Complete = "DOWNLOAD_COMPLETE_PATH";
    public const string Download_Path_Incomplete = "DOWNLOAD_INCOMPLETE_PATH";
}
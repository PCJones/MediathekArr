using Microsoft.AspNetCore.Mvc;

namespace MediathekArr.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DownloadController : ControllerBase
    {
        [HttpGet("api")]
        public IActionResult GetVersion([FromQuery] string mode)
        {
            switch (mode)
            {
                case "version":
                    return Ok(new { version = "4.3.3" });
                case "get_config":
                    return Content(GetConfigResponse(), "application/json");
                default:
                    return BadRequest(new { error = "Invalid mode" });
            }
        }

        private static string GetConfigResponse()
        {
            return @"{
    ""config"": {
        ""misc"": {
            ""complete_dir"": ""C:/private/MediathekArr/MediathekArr/bin/Debug/net8.0/downloads/sonarr"",
            ""enable_tv_sorting"": false,
            ""enable_movie_sorting"": false,
            ""pre_check"": false,
            ""history_retention"": ""all""
        },
        ""categories"": [
            {
                ""name"": ""sonarr"",
                ""pp"": """",
                ""script"": ""Default"",
                ""dir"": """",
                ""priority"": -100
            },
            {
                ""name"": ""movies"",
                ""pp"": """",
                ""script"": ""Default"",
                ""dir"": """",
                ""priority"": -100
            }
        ],
        ""sorters"": []
    }
}
";
        }
    }
}

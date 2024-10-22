using MediathekArr.Models;
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
            return mode switch
            {
                "version" => Ok(new { version = "4.3.3" }),
                "get_config" => Content(GetConfigResponse(), "application/json"),
                "queue" => Ok(GetDummyQueue()), // Handle the queue mode
                _ => BadRequest(new { error = "Invalid mode" }),
            };
        }

        private static QueueWrapper GetDummyQueue()
        {

            // Create a dummy queue item
            var dummyItem = new SabnzbdQueueItem
            {
                Status = "Downloading", // Simulating downloading status
                Index = 0,
                Timeleft = "0:10:00", // 10 minutes remaining
                Size = "1163.54", // 1163.54 MB total size
                Title = "Dummy Title", // Dummy title
                Category = "sonarr", // Dummy category
                Sizeleft = "200.5", // 200.5 MB left
                Percentage = "80", // 80% complete
                Id = System.Guid.NewGuid().ToString() // Random ID
            };

            // Create a SabnzbdQueue object with the dummy item
            var queue = new SabnzbdQueue
            {
                Items = new List<SabnzbdQueueItem> { dummyItem }
            };

            // Wrap the SabnzbdQueue inside the QueueWrapper
            return new QueueWrapper
            {
                Queue = queue
            };
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

using Microsoft.AspNetCore.Mvc;

namespace MediathekArr.Controllers;

[ApiController]
[Route("api")]
public class TProxyController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    // Route when there is no path
    [HttpGet("")]
    [HttpPost("")]
    [HttpPut("")]
    [HttpDelete("")]
    [HttpPatch("")]
    public async Task<IActionResult> ProxyRoot()
    {
        return await Proxy(string.Empty);
    }

    // Route when there is a path
    [HttpGet("{*path}")]
    [HttpPost("{*path}")]
    [HttpPut("{*path}")]
    [HttpDelete("{*path}")]
    [HttpPatch("{*path}")]
    public async Task<IActionResult> Proxy(string path)
    {
        var queryString = Request.QueryString.Value;
        var targetUrl = $"http://localhost:5008/api/{path}{queryString}";

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(Request.Method),
            RequestUri = new Uri(targetUrl),
            Content = Request.ContentLength > 0 ? new StreamContent(Request.Body) : null
        };

        foreach (var header in Request.Headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
        }

        var responseMessage = await _httpClient.SendAsync(requestMessage);

        var responseContent = await responseMessage.Content.ReadAsStringAsync();
        return new ContentResult
        {
            Content = responseContent,
            StatusCode = (int)responseMessage.StatusCode,
            ContentType = responseMessage.Content.Headers.ContentType?.ToString()
        };
    }
}

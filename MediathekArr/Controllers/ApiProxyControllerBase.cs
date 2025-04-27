using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

namespace MediathekArrDownloader.Controllers;

public abstract class ApiProxyControllerBase : ControllerBase
{
    protected readonly IHttpClientFactory HttpClientFactory;

    protected ApiProxyControllerBase(IHttpClientFactory httpClientFactory)
    {
        HttpClientFactory = httpClientFactory;
    }

    protected IActionResult? ValidateRequiredParameters(string apiKey, string host, string hostName = "Host")
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(host))
        {
            return BadRequest($"{hostName} and API key are required.");
        }
        return null;
    }

    protected HttpClient CreateHttpClient(string apiKey)
    {
        var httpClient = HttpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return httpClient;
    }

    protected async Task<string> ReadRequestBodyAsync()
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync();
    }

    protected IActionResult HandleErrorResponse(HttpStatusCode statusCode, string responseContent, string errorContext)
    {
        string message = $"Error {errorContext}";

        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            try
            {
                // Try to parse as array of validation errors
                var errorArray = JsonConvert.DeserializeObject<List<JObject>>(responseContent);
                if (errorArray?.Count > 0)
                {
                    var errorMessages = errorArray
                        .Select(err => (string)err["errorMessage"])
                        .Where(msg => !string.IsNullOrEmpty(msg))
                        .ToList();

                    if (errorMessages.Count != 0)
                    {
                        message = string.Join(", ", errorMessages);
                        var propertyName = errorArray.Select(err => (string)err["propertyName"]).FirstOrDefault();
                        var attemptedValue = errorArray.Select(err => (string)err["attemptedValue"]).FirstOrDefault();
                        if (propertyName is not null)
                        {
                            message += $":{Environment.NewLine}Property Name: \"{propertyName}\"{Environment.NewLine}Attempted Value: \"{attemptedValue}\"";
                        }
                    }
                }
                else
                {
                    // Try to parse as a single error object with errorMessage property
                    var errorObj = JsonConvert.DeserializeObject<JObject>(responseContent);
                    if (errorObj?["errorMessage"] != null)
                    {
                        message = errorObj["errorMessage"].ToString();
                    }
                }
            }
            catch
            {
                // If parsing fails, fall back to raw response content
                message = responseContent;
            }
        }

        var errorResult = new
        {
            error = new
            {
                message
            }
        };

        return StatusCode((int)statusCode, errorResult);
    }


    protected async Task<IActionResult> ExecuteApiRequest(
        string apiKey,
        string host,
        string endpoint,
        HttpMethod method,
        string errorContext,
        Func<string, IActionResult> processSuccessResponse = null,
        string hostName = "Host",
        bool isProwlarr = false)
    {
        var validationResult = ValidateRequiredParameters(apiKey, host, hostName);
        if (validationResult != null)
            return validationResult;

        var cleanedHostName = host.TrimEnd('/');
        var httpClient = CreateHttpClient(apiKey);

        try
        {
            StringContent content = null;

            if (method != HttpMethod.Get)
            {
                var rawBody = await ReadRequestBodyAsync();
                content = new StringContent(rawBody, System.Text.Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;

            if (method == HttpMethod.Get)
            {
                response = await httpClient.GetAsync($"{cleanedHostName}/{endpoint}");
            }
            else if (method == HttpMethod.Post)
            {
                response = await httpClient.PostAsync($"{cleanedHostName}/{endpoint}", content);
            }
            else // PUT
            {
                response = await httpClient.PutAsync($"{cleanedHostName}/{endpoint}", content);
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return HandleErrorResponse(response.StatusCode, responseContent, errorContext);
            }

            if (processSuccessResponse != null)
            {
                return processSuccessResponse(responseContent);
            }

            return Content(responseContent, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error {errorContext}: {ex.Message}");
        }
    }
}
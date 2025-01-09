using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tvdb.Clients;
using Tvdb.Models;
using Xunit.Abstractions;

namespace Tvdb;

public class AuthenticationUnitTests
{
    public AuthenticationUnitTests(ITestOutputHelper outputHelper)
    {
        OutputHelper = outputHelper;

        var builder = new HostApplicationBuilder();
        var config = builder.Configuration
            .AddTvdbClient()
            .Build();
        ServiceProvider = builder.Services
            .AddLogging((builder) => builder.AddXUnit(OutputHelper))
            .AddTvdbClient(config)
            .BuildServiceProvider();
    }

    public ITestOutputHelper OutputHelper { get; }
    public ServiceProvider ServiceProvider { get; internal set; }

    [Fact]
    public async void ManuallyAuthenticate_Fact()
    {
        // Arrange
        var config = new Configuration.TvdbConfiguration
        {
            BaseUrl = "https://api4.thetvdb.com/v4",
            ApiKey = "e82b72fe-8674-4cc1-8cbb-474ed12a3fed"
        };
        var httpClient = new HttpClient();
        var requestBody = new StringContent(JsonSerializer.Serialize(new { apikey = config.ApiKey }), System.Text.Encoding.UTF8, System.Net.Mime.MediaTypeNames.Application.Json);

        // Act
        var response = await httpClient.PostAsync(config.TokenUrl, requestBody);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponseWrapper<Token>>(responseData);

        // Assert

        /* Validate Wrapper, Response should be a success */
        result.Should().NotBeNull();
        result.Status.Should().Be("success");
        result.IsSuccess.Should().BeTrue();

        /* Validate Data, Token should be populated and valid for a month */
        result.Data.Should().NotBeNull();
        var token = result.Data;
        token.TokenType.Should().Be("Bearer");
        token.CreationTimestamp.Should().BeBefore(DateTime.Now);
        token.IsTokenExpired.Should().BeFalse();
        token.TokenExpiryDate.Should().BeCloseTo(DateTime.Today.AddMonths(1), TimeSpan.FromDays(1)); // should be roughly a month, +/- a day
        
        token.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async void TestRandomApi_Fact()
    {
        var client = ServiceProvider.GetRequiredService<ICountriesClient>();
        var result = await client.CountriesAsync();
        result.Status.Should().Be("success");
    }
}
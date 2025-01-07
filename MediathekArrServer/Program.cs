using MediathekArrLib.Utilities;
using MediathekArrServer.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Logging.AddMediathekArrLogging();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("MediathekClient", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:131.0) Gecko/20100101 Firefox/131.0");
    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
})
.AddHttpMessageHandler<MediathekArrLoggingHandler>(); // Add sensitive query parameters to log output
builder.Services.TryAddTransient<MediathekArrLoggingHandler>();

builder.Services.AddHostedService<RulesetBackgroundService>();
builder.Services.AddSingleton<MediathekSearchService>();
builder.Services.AddSingleton<ItemLookupService>();

var app = builder.Build();

// Middleware to log all incoming requests
app.Use(async (context, next) =>
{
    // Log the incoming request details
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var request = context.Request;
    logger.LogInformation("Incoming Request: {method} {url}", request.Method, request.Path + request.QueryString);

    // Check if the request is a POST and has a body
    if (request.Method == HttpMethods.Post && request.ContentLength > 0)
    {
        // Enable buffering so the request can be read multiple times
        request.EnableBuffering();
    }

    // Call the next middleware in the pipeline
    await next.Invoke();
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
using MediathekArrLib.Utilities;
using MediathekArrServer.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Logging.AddMediathekArrLogger();
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
.AddHttpMessageHandler<HttpClientLoggingHandler>(); // Add sensitive query parameters to log output
builder.Services.TryAddTransient<HttpClientLoggingHandler>();

builder.Services.AddHostedService<RulesetBackgroundService>();
builder.Services.AddSingleton<MediathekSearchService>();
builder.Services.AddSingleton<ItemLookupService>();

var app = builder.Build();

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
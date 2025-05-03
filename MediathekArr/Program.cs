using MediathekArrLib.Utilities;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MediathekArrDownloader.Services;
using MediathekArrDownloader.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false;
    options.SingleLine = false;
});

builder.Services.AddSingleton(provider =>
{
    var configuration = builder.Configuration;
    var logger = provider.GetRequiredService<ILogger<Program>>();
    return ConfigureAppConfig(configuration, logger);
});

builder.Services.AddControllers();
builder.Logging.AddMediathekArrLogger();
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("MediathekClient", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0");
    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
	client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
	client.DefaultRequestHeaders.Accept.ParseAdd("text/plain");
	client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
})
.AddHttpMessageHandler<HttpClientLoggingHandler>(); // Add sensitive query parameters to log output
builder.Services.TryAddTransient<HttpClientLoggingHandler>();
builder.Services.AddSingleton<DownloadService>();

var app = builder.Build();

// Middleware to redirect "/" to "/download"
AddRedirectMiddleware(app);

// Middleware to log all incoming requests
AddIncomingRequestsLogMiddleware(app);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();

app.MapControllers();

AddStaticFilesMiddleware(app);

// Force instantiation of DownloadService
using (var scope = app.Services.CreateScope())
{
    var downloadService = scope.ServiceProvider.GetRequiredService<DownloadService>();
}
app.Run();

static Config ConfigureAppConfig(IConfiguration configuration, ILogger logger)
{
    var configPathEnv = Environment.GetEnvironmentVariable("CONFIG_PATH");

    string configFilePath;
    if (!string.IsNullOrEmpty(configPathEnv))
    {
        configFilePath = Path.Combine(configPathEnv, "mediathekarr.json");
    }
    else
    {
        configFilePath = Path.Combine("config", "mediathekarr.json");
    }
    logger.LogInformation("Config path set to: {configFilePath}", configFilePath);

    bool existingConfig;
    Config config;

    if (File.Exists(configFilePath))
    {
        var jsonContent = File.ReadAllText(configFilePath);
        config = System.Text.Json.JsonSerializer.Deserialize<Config>(jsonContent) ?? new Config();
        logger.LogInformation("Loaded configuration from {configFilePath}", configFilePath);
        existingConfig = true;
    }
    else
    {
        config = new Config();
        existingConfig = false;
    }

    // Override from environment variables
    var incompletePath = Environment.GetEnvironmentVariable("DOWNLOAD_INCOMPLETE_PATH");
    var completePath = Environment.GetEnvironmentVariable("DOWNLOAD_COMPLETE_PATH");
    var categoriesRaw = Environment.GetEnvironmentVariable("CATEGORIES");

    if (!string.IsNullOrEmpty(incompletePath))
    {
        logger.LogInformation("Overriding incomplete path from environment variable: {IncompletePath}", incompletePath);
        config.IncompletePath = incompletePath;
    }
    else if (!existingConfig)
    {
        config.IncompletePath = GetDefaultPath(AppContext.BaseDirectory, "incomplete", logger);
    }

    if (!string.IsNullOrEmpty(completePath))
    {
        logger.LogInformation("Overriding complete path from environment variable: {CompletePath}", completePath);
        config.CompletePath = completePath;
    }
    else if (!existingConfig)
    {
        config.CompletePath = GetDefaultPath(AppContext.BaseDirectory, "complete", logger);
    }

    if (!string.IsNullOrEmpty(categoriesRaw))
    {
        logger.LogInformation("Overriding categories from environment variable: {categoriesRaw}", categoriesRaw);
        config.Categories = categoriesRaw.Split(',');
    }

    if (!existingConfig && (string.IsNullOrEmpty(incompletePath) || string.IsNullOrEmpty(completePath) || string.IsNullOrEmpty(categoriesRaw)))
    {
        logger.LogWarning("Attention!");
        logger.LogWarning("Configuration file was not found or is incomplete. Please visit http://localhost:5007/ to setup MediathekArr.");
        logger.LogWarning("Alternatively use environment variables (see https://github.com/PCJones/MediathekArr).");
        logger.LogWarning("MediathekArr will use default values:");
    }

    logger.LogInformation("MediathekArr Configuration: {Config}", System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
    {
        WriteIndented = true
    }));


    return config;
}


static string GetDefaultPath(string startupPath, string type, ILogger logger)
{
    string defaultPath;

    if (OperatingSystem.IsWindows())
    {
        defaultPath = Path.Combine(startupPath, "downloads", type);
        if (!Directory.Exists(defaultPath))
        {
            Directory.CreateDirectory(defaultPath);
        }
    }
    else
    {
        defaultPath = $"/data/mediathek/{type}";
        if (!Directory.Exists(defaultPath))
        {
            Directory.CreateDirectory(defaultPath);
        }
    }

    logger.LogWarning("Using default {Type} path: {DefaultPath}", type, defaultPath);
    return defaultPath;
}

static void AddRedirectMiddleware(WebApplication app)
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/" && string.IsNullOrEmpty(context.Request.QueryString.Value))
        {
            context.Response.Redirect("/download", permanent: true);
            return;
        }

        await next();
    });
}

static void AddIncomingRequestsLogMiddleware(WebApplication app)
{
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
}

static void AddStaticFilesMiddleware(WebApplication app)
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            Path.Combine(Directory.GetCurrentDirectory(), "static", "download")
        ),
        RequestPath = "/download"
    });

    app.MapGet("/download", async context =>
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "static", "download", "index.html");
        var content = await File.ReadAllTextAsync(path);
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(content);
    });
}
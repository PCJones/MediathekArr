using MediathekArr.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
});
builder.Services.AddSingleton<MediathekSearchService>();
builder.Services.AddSingleton<ItemLookupService>();
builder.Services.AddSingleton<DownloadService>();


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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

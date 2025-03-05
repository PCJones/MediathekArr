using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace MediathekArrLib.Utilities;

/// <summary>
/// Generic Logger for MediathekArr
/// </summary>
public class MediathekArrLogger : ILogger
{
    private readonly string _name;
    private readonly ColourConsoleLoggerConfiguration _config;

    public MediathekArrLogger(string name, ColourConsoleLoggerConfiguration config)
    {
        _name = name;
        _config = config;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
        var originalTextColour = Console.ForegroundColor;
        Console.ForegroundColor = _config.LogLevelTextColourMapping.TryGetValue(logLevel, out ConsoleColor loggingTextColour) ? loggingTextColour : originalTextColour;
        string logLevelString = _config.LogLevelAbbreviationMapping.TryGetValue(logLevel, out string logLevelAbbreviation) ? logLevelAbbreviation : logLevel.ToString();

        string message = formatter(state, exception);

        // Append exception details if an exception exists
        if (exception != null)
        {
            message += Environment.NewLine + exception.ToString();
        }

        Console.WriteLine($"[{assemblyName}] {logLevelString}: {_name} - {message}");
        Console.ForegroundColor = originalTextColour;
    }
}

/// <summary>
/// ILogger Provider for <see cref="MediathekArrLogger"/>
/// </summary>
/// <param name="config"></param>
public class MediathekArrLoggerProvider(ColourConsoleLoggerConfiguration config) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, MediathekArrLogger> _loggers = new();

    public ColourConsoleLoggerConfiguration Config { get; } = config;

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new MediathekArrLogger(name, Config));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}


/// <summary>
/// Console Colour configuration
/// </summary>
public class ColourConsoleLoggerConfiguration
{
    public Dictionary<LogLevel, ConsoleColor> LogLevelTextColourMapping { get; set; } = new Dictionary<LogLevel, ConsoleColor>
    {
        [LogLevel.Trace] = ConsoleColor.Gray,
        [LogLevel.Debug] = ConsoleColor.Blue,
        [LogLevel.Information] = ConsoleColor.White,
        [LogLevel.Warning] = ConsoleColor.DarkYellow,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Critical] = ConsoleColor.Magenta
    };

    public Dictionary<LogLevel, string> LogLevelAbbreviationMapping { get; set; } = new Dictionary<LogLevel, string>
    {
        [LogLevel.Trace] = "trce",
        [LogLevel.Debug] = "dbug",
        [LogLevel.Information] = "info",
        [LogLevel.Warning] = "warn",
        [LogLevel.Error] = "fail",
        [LogLevel.Critical] = "crit"
    };
}

/// <summary>
/// Extensions to inject MediathekArr Logger
/// </summary>
public static class LoggerExtensions
{
    public static ILoggingBuilder AddMediathekArrLogger(this ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddProvider(new MediathekArrLoggerProvider(new ColourConsoleLoggerConfiguration()));
        return builder;
    }
}

/// <summary>
/// Logging Handler for HttpClient
/// </summary>
/// <param name="logger"></param>
public class HttpClientLoggingHandler(ILogger<HttpClientLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start processing HTTP request {Method} {Url}", request.Method, request.RequestUri);
        var response = await base.SendAsync(request, cancellationToken);
        logger.LogInformation("Finished processing HTTP request {Method} {Url}", request.Method, request.RequestUri);
        return response;
    }
}
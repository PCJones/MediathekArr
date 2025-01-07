using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace MediathekArrLib.Utilities;

public class MediathekArrConsoleFormatter : ConsoleFormatter
{
    public const string FormatterName = "MediathekArr";

    public MediathekArrConsoleFormatter() : base(FormatterName) { } // Give this Formatter a name

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        string assemblyName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name; // Get Display name of Assembly, allowing us to do some nice formatting
        textWriter.WriteLine($"{DateTime.Now:dd/MM/yy HH:mm:ss.fff}: [{assemblyName}] {logEntry.LogLevel}");
        textWriter.WriteLine($"{logEntry.Formatter(logEntry.State, logEntry.Exception)}")
    }
}

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
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
        var originalColour = Console.ForegroundColor;
        Console.ForegroundColor = _config.LogLevelMapping[logLevel];
        Console.WriteLine($"{DateTime.Now:dd/MM/yy HH:mm:ss.fff}: [{assemblyName}] {logLevel} - {_name}");
        Console.ForegroundColor = originalColour;
        Console.WriteLine($"{formatter(state, exception)}");
    }
}

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

public class MediathekArrLoggingHandler(ILogger<MediathekArrLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start processing HTTP request {Method} {Url}", request.Method, request.RequestUri);
        var response = await base.SendAsync(request, cancellationToken);
        logger.LogInformation("Finished processing HTTP request {Method} {Url}", request.Method, request.RequestUri);
        return response;
    }
}

public class ColourConsoleLoggerConfiguration
{
    public Dictionary<LogLevel, ConsoleColor> LogLevelMapping { get; set; } = new Dictionary<LogLevel, ConsoleColor>
    {
        [LogLevel.Information] = ConsoleColor.White,
        [LogLevel.Warning] = ConsoleColor.DarkYellow,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Critical] = ConsoleColor.Magenta
    };
}

public static class LoggerExtensions
{
    public static ILoggingBuilder AddMediathekArrLogging(this ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddProvider(new MediathekArrLoggerProvider(new ColourConsoleLoggerConfiguration()));
        //builder.AddConsole(options => options.FormatterName = MediathekArrConsoleFormatter.FormatterName);
        //builder.AddConsoleFormatter<MediathekArrConsoleFormatter, ConsoleFormatterOptions>();
        return builder;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediathekArr.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tvdb.Configuration;
using Tvdb.Handlers;
using Tvdb.Provider;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrapper
{
    public static IConfigurationBuilder AddTvdbClient(this IConfigurationBuilder builder)
    {
        var config = builder
            .AddJsonFile("TvdbClientConfig.json", optional: true)
            .Build();

        return builder;
    }

    public static IServiceCollection AddTvdbClient(this IServiceCollection builder, IConfiguration config)
    {
        /* Inject TVDB Clients */
        builder.Configure<TvdbConfiguration>(config.GetRequiredSection("TvdbConfiguration"));
        builder.TryAddSingleton<ITokenProvider, TvdbTokenProvider>();
        builder.TryAddTransient<TokenAuthorizationHeaderHandler>();

        string baseUrl = config.GetValue<string>("TvdbConfiguration:BaseUrl") ?? "https://api4.thetvdb.com/v4";
        builder
            .AddHttpClient(Tvdb.Constants.TvdbConstants.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(baseUrl.EnsureTrailingSlash());
            })
            .AddHttpMessageHandler<TokenAuthorizationHeaderHandler>();

        /* Inject all Tvdb Clients at once Clients */
        builder.Scan(scan => scan
        .FromCallingAssembly()
        .AddClasses(classes => classes.AssignableTo<Tvdb.Clients.ITvdbClient>())
        .AsMatchingInterface()
        .AsHttpClient(Tvdb.Constants.TvdbConstants.HttpClientName)
        );

        return builder;
    }

    /// <summary>
    /// Ensure that the input string ends on a slash
    /// </summary>
    /// <param name="inputString"></param>
    /// <returns></returns>
    public static string EnsureTrailingSlash(this string inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString)) return string.Empty;

        if (!inputString.EndsWith("/")) inputString += "/";
        return inputString;
    }
}

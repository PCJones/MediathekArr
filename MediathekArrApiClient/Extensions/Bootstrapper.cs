using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediathekArr.Clients;
using MediathekArr.Configuration;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrapper
{
    public static IConfigurationBuilder AddTvdbClient(this IConfigurationBuilder builder)
    {
        var config = builder
            .AddJsonFile("MediathekArrApiClientConfig.json", optional: true)
            .Build();

        return builder;
    }

    public static IServiceCollection AddMediathekArrApiClient(this IServiceCollection builder, IConfiguration config)
    {
        /* Inject MediathekArr API Clients */
        builder.Configure<MediathekArrApiConfiguration>(config.GetRequiredSection("MediathekArr"));

        string baseUrl = config.GetValue<string>("Api:BaseUrl") ?? "http://localhost:5036";
        builder
            .AddHttpClient(MediathekArr.Constants.MediathekArrApiClientConstants.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(baseUrl.EnsureTrailingSlash());
            });

        /* Inject all MediathekArr Clients at once */
        builder.Scan(scan => scan
        .FromCallingAssembly()
        .AddClasses(classes => classes.AssignableTo<IMediathekArrApiClient>())
        .AsMatchingInterface()
        .AsHttpClient(MediathekArr.Constants.MediathekArrApiClientConstants.HttpClientName)
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

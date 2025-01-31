using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediathekArr.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace MediathekArr.Controllers;

public class SeriesControllerUnitTests
{
    public SeriesControllerUnitTests(ITestOutputHelper outputHelper)
    {
        OutputHelper = outputHelper;

        var builder = new HostApplicationBuilder();
        
        var config = builder.Configuration
            .AddTvdbClient()
            .Build();

        builder.Services.AddDbContext<MediathekArrContext>(options => options.UseSqlite("Data Source=tvdb_cache.sqlite"));

        builder.Services
            .AddLogging((builder) => builder.AddXUnit(OutputHelper))
            .AddTvdbClient(config);

        builder.Services.TryAddSingleton<SeriesController>();

        ServiceProvider = builder.Services.BuildServiceProvider();

        var dbContext = ServiceProvider.GetRequiredService<MediathekArrContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        Controller = ServiceProvider.GetRequiredService<SeriesController>();
    }

    public ITestOutputHelper OutputHelper { get; }
    public ServiceProvider ServiceProvider { get; }
    public SeriesController Controller { get; }

    [Theory]
    [InlineData(234791)] // Heute Show
    public async Task GetSeriesData_Theory(int tvdbId)
    {
        // Arrange

        // Act
        var result = await Controller.GetSeriesData(tvdbId);
        // Assert
        result.Should().NotBeNull();
    }
}

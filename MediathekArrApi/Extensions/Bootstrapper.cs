using System.Text.Json.Serialization;
using MediathekArr.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrapper
{
    public static IConfigurationBuilder AddMediathekArrApi(this IConfigurationBuilder builder)
    {
        return builder;
    }

    public static WebApplicationBuilder AddMediathekArrApi(this WebApplicationBuilder builder)
    {
        builder.Logging.AddMediathekArrLogger();

        #region Caching
        builder.Services.AddMemoryCache();
        /* Caching architecture:
         * Ideally we build up some form of caching across all components. There are basically two different types of caching,
         * internal caching within the application and outgoing caching to the client.
         * 
         * Internal Caching:
         * https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-9.0
         * 
         * Output Caching:
         * https://learn.microsoft.com/en-us/aspnet/core/performance/caching/overview?view=aspnetcore-9.0#output-caching
         * https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output?view=aspnetcore-9.0
         * https://learn.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-9.0
         * 
         * Distributed Cache:
         * https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-9.0
         * https://github.com/leonibr/community-extensions-cache-postgres
         * https://github.com/neosmart/SqliteCache
         * 
         * Hybrid Cache (Still in Preview, combines local and distributed cache):
         * https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid?view=aspnetcore-9.0
         */
        #endregion

        #region Database
        /* Add DbContext with specific DB Implementation 
         * Uncomment whatever Database you want to use and comment the other one(s) out :-)
         */

        /* Postgres SQL */
        //builder.Services.AddDbContext<MediathekArrContext>(options => options.UseNpgsql("Host=localhost;Database=tvdb_cache;Username=yourusername;Password=yourpassword"));

        /* SQLite */
        builder.Services.AddDbContext<MediathekArrContext>(options => options.UseSqlite("Data Source=tvdb_cache.sqlite"));
        #endregion

        #region TVDB Client
        /* Spin up the TVDB Client */
        var config = builder.Configuration.AddTvdbClient().Build();
        builder.Services.AddTvdbClient(config);
        #endregion

        /* Prevent circular endless loops */
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        }); ;
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        return builder;
    }

    public static IApplicationBuilder AddMediathekArrApi(this WebApplication app)
    {
        /* Spin up Database if first start */
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MediathekArrContext>();
        dbContext.Database.EnsureCreated();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}

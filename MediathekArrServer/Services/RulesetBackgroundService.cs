namespace MediathekArr.Services;

using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

public class RulesetBackgroundService(IServiceProvider serviceProvider, ILogger<RulesetBackgroundService> logger) : BackgroundService
{
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var searchService = scope.ServiceProvider.GetRequiredService<MediathekSearchService>();

                try
                {
                    logger.LogInformation("Starting ruleset update at {Time}", DateTime.UtcNow);
                    await searchService.UpdateRulesetsAsync();
                    logger.LogInformation("Ruleset update completed successfully at {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating rulesets at {Time}", DateTime.UtcNow);
                }
            }

            await Task.Delay(_refreshInterval, stoppingToken);
        }
    }
}

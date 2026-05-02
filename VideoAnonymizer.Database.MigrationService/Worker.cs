using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoAnonymizer.Database.Extensions;

namespace VideoAnonymizer.Database.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        await serviceProvider.ApplyDatabaseMigrationsAsync(configuration);
        hostApplicationLifetime.StopApplication();
    }
}

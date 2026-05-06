using Microsoft.EntityFrameworkCore;

namespace VideoAnonymizer.Database.Extensions;

public static class VideoAnonymizerDbContextExtensions
{
    public static async Task MigrateDatabaseAsync(this VideoAnonymizerDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    public static void SetDevelopmentOptions(this DbContextOptionsBuilder options)
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
}

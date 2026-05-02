using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VideoAnonymizer.Database.Extensions;

public static class ServiceProviderExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"];

        using var scope = serviceProvider.CreateScope();

        if (string.Equals(provider, DatabaseProvider.Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<VideoAnonymizerDbContext>>();
            using var db = await dbFactory.CreateDbContextAsync();
            await db.MigrateDatabaseAsync();
        }
        else
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<VideoAnonymizerDbContext>();
            await dbContext.MigrateDatabaseAsync();
        }
    }
}

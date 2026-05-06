using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoAnonymizer.Database.Extensions;

namespace VideoAnonymizer.Database.SQLite.Extensions;

public static class HostApplicationBuilderExtensions
{
    public static void AddSqliteVideoAnonymizerDbContext(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContext<VideoAnonymizerDbContext>((sp, options) =>
        {
            ConfigureSqliteOptions(builder.Configuration, options);
        });
    }

    public static void AddSqliteVideoAnonymizerDbContextFactory(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<VideoAnonymizerDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            ConfigureSqliteOptions(configuration, options);
        });
    }

    private static void ConfigureSqliteOptions(IConfiguration configuration, DbContextOptionsBuilder options)
    {
        var filePath = configuration["Database:FilePath"]
            ?? Path.Combine("App_Data", "videoanonymizer.db");
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        options.UseSqlite($"Data Source={filePath}", b => b.MigrationsAssembly("VideoAnonymizer.Database.SQLite"));
    }
}

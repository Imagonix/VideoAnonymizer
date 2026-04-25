using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace VideoAnonymizer.Database.Extensions
{
    public static class HostApplicationBuilderExtensions
    {
        private static string _connectionString = "videoAnonymizerDb";

        public static void AddVideoAnonymizerDbContext(this IHostApplicationBuilder builder)
        {
            if (UsesInMemoryDatabase(builder))
            {
                builder.Services.AddSingleton<InMemoryDatabaseRoot>();
                builder.Services.AddDbContext<VideoAnonymizerDbContext>((sp, options) =>
                {
                    ConfigureDbContextOptions(builder, sp, options);
                });
                return;
            }

            builder.AddNpgsqlDbContext<VideoAnonymizerDbContext>(_connectionString, configureDbContextOptions: options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    SetDevelopmentOptions(options);
                }

                options.UseNpgsql(o => o.CommandTimeout(500));
            });
        }

        public static void AddVideoAnonymizerDbContextFactory(this IHostApplicationBuilder builder)
        {
            if (UsesInMemoryDatabase(builder))
            {
                builder.Services.AddSingleton<InMemoryDatabaseRoot>();
            }

            builder.Services.AddDbContextFactory<VideoAnonymizerDbContext>((sp, options) =>
            {
                ConfigureDbContextOptions(builder, sp, options);
            });
        }

        private static void ConfigureDbContextOptions(
            IHostApplicationBuilder builder,
            IServiceProvider serviceProvider,
            DbContextOptionsBuilder options)
        {
            if (UsesInMemoryDatabase(builder))
            {
                var databaseName = builder.Configuration["Database:InMemoryName"]
                    ?? "VideoAnonymizerStandalone";
                var databaseRoot = serviceProvider.GetRequiredService<InMemoryDatabaseRoot>();
                options.UseInMemoryDatabase(databaseName, databaseRoot);
                return;
            }

            var cs = builder.Configuration.GetConnectionString(_connectionString)
                     ?? throw new InvalidOperationException(
                         $"Connection string '{_connectionString}' not found");

            var csb = new NpgsqlConnectionStringBuilder(cs);
            if (builder.Environment.IsDevelopment())
            {
                csb.IncludeErrorDetail = true;
                SetDevelopmentOptions(options);
            }

            options.UseNpgsql(csb.ConnectionString, o => o.CommandTimeout(500));
        }

        private static bool UsesInMemoryDatabase(IHostApplicationBuilder builder)
        {
            return string.Equals(
                builder.Configuration["Database:Provider"],
                DatabaseProvider.InMemory,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void SetDevelopmentOptions(DbContextOptionsBuilder options)
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    }
}

using Microsoft.EntityFrameworkCore;
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
            if (UsesSqliteDatabase(builder))
            {
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

                options.UseNpgsql(o =>
                {
                    o.CommandTimeout(500);
                    o.MigrationsAssembly("VideoAnonymizer.Database.Postgres");
                });
            });
        }

        public static void AddVideoAnonymizerDbContextFactory(this IHostApplicationBuilder builder)
        {
            if (UsesSqliteDatabase(builder))
            {
                builder.Services.AddDbContextFactory<VideoAnonymizerDbContext>((sp, options) =>
                {
                    ConfigureDbContextOptions(builder, sp, options);
                });
                return;
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
            if (UsesSqliteDatabase(builder))
            {
                var filePath = builder.Configuration["Database:FilePath"]
                    ?? Path.Combine("App_Data", "videoanonymizer.db");
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                options.UseSqlite($"Data Source={filePath}", b => b.MigrationsAssembly("VideoAnonymizer.Database.SQLite"));
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

            options.UseNpgsql(csb.ConnectionString, o =>
            {
                o.CommandTimeout(500);
                o.MigrationsAssembly("VideoAnonymizer.Database.Postgres");
            });
        }

        private static bool UsesSqliteDatabase(IHostApplicationBuilder builder)
        {
            return string.Equals(
                builder.Configuration["Database:Provider"],
                DatabaseProvider.Sqlite,
                StringComparison.OrdinalIgnoreCase);
        }

        private static void SetDevelopmentOptions(DbContextOptionsBuilder options)
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }

        public static async Task MigrateDatabaseAsync(this VideoAnonymizerDbContext dbContext, CancellationToken cancellationToken = default)
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            });
        }

    }
}

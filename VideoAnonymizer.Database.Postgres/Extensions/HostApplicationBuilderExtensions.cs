using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using VideoAnonymizer.Database.Extensions;

namespace VideoAnonymizer.Database.Postgres.Extensions;

public static class HostApplicationBuilderExtensions
{
    private const string ConnectionStringName = "videoAnonymizerDb";

    public static void AddPostgresVideoAnonymizerDbContext(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<VideoAnonymizerDbContext>(ConnectionStringName, configureDbContextOptions: options =>
        {
            if (builder.Environment.IsDevelopment())
            {
                options.SetDevelopmentOptions();
            }

            options.UseNpgsql(o =>
            {
                o.CommandTimeout(500);
                o.MigrationsAssembly("VideoAnonymizer.Database.Postgres");
            });
        });
    }

    public static void AddPostgresVideoAnonymizerDbContextFactory(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<VideoAnonymizerDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            ConfigurePostgresOptions(configuration, builder, options);
        });
    }

    private static void ConfigurePostgresOptions(IConfiguration configuration, IHostApplicationBuilder builder, DbContextOptionsBuilder options)
    {
        var cs = configuration.GetConnectionString(ConnectionStringName)
                 ?? throw new InvalidOperationException(
                     $"Connection string '{ConnectionStringName}' not found");

        var csb = new NpgsqlConnectionStringBuilder(cs);
        if (builder.Environment.IsDevelopment())
        {
            csb.IncludeErrorDetail = true;
            options.SetDevelopmentOptions();
        }

        options.UseNpgsql(csb.ConnectionString, o =>
        {
            o.CommandTimeout(500);
            o.MigrationsAssembly("VideoAnonymizer.Database.Postgres");
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Database.Extensions
{
    public static class HostApplicationBuilderExtensions
    {
        private static string _connectionString = "videoAnonymizerDb";

        public static void AddVideoAnonymizerDbContext(this IHostApplicationBuilder builder)
        {
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
            builder.Services.AddDbContextFactory<VideoAnonymizerDbContext>(options =>
            {
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
            });
        }

        private static void SetDevelopmentOptions(DbContextOptionsBuilder options)
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using VideoAnonymizer.Contracts.RabbitMQ;

namespace VideoAnonymizer.Contracts.Extensions
{
    public static class HostApplicationBuilderExtensions
    {
        public static void ConfigureRabbitMQConnection(this IHostApplicationBuilder builder)
        {
            var rabbitConnectionString = builder.Configuration.GetConnectionString("rabbit")
                ?? throw new InvalidOperationException("RabbitMQ connection string 'messaging' not found.");

            builder.Services.Configure<RabbitMqOptions>(options =>
            {
                options.ConnectionString = rabbitConnectionString;
                options.ExchangeName = "video-anonymizer";
            });
        }
    }
}

using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Contracts
{
    public static class IBusRegistrationConfiguratorExtensions
    {
        public static void ConfigureRabbitMq(this IBusRegistrationConfigurator configurator, IHostApplicationBuilder builder)
        {
            configurator.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = builder.Configuration.GetConnectionString("rabbit");
                cfg.Host(new Uri(rabbit!));
                cfg.ConfigureEndpoints(context);
            });
        }
    }
}

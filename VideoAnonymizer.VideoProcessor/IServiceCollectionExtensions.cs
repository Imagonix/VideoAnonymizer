using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;

namespace VideoAnonymizer.VideoProcessor
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddVideoProcessorWorkers(this IServiceCollection services)
        {
            services.AddSingletonAsHostedService<VideoAnalyzer>();
            services.AddSingletonAsHostedService<VideoAnonymizer>();
            return services;
        }

        public static IServiceCollection AddVideoProcessorMessageHandlers(this IServiceCollection services)
        {
            services.AddSingleton<IMessageHandler<AnalyzeVideo>, AnalyzeVideoHandler>();
            services.AddSingleton<IMessageHandler<AnonymizeVideo>, AnonymizeVideoHandler>();
            return services;
        }

        public static IServiceCollection AddRabbitMqVideoProcessorConsumers(this IServiceCollection services)
        {
            services.AddHostedService<AnalyzeVideoConsumer>();
            services.AddHostedService<AnonymizeVideoConsumer>();
            return services;
        }

        public static void AddSingletonAsHostedService<T>(this IServiceCollection services)
            where T : class, IHostedService
        {
            services.AddSingleton<T>();
            services.AddHostedService(sp => sp.GetRequiredService<T>());
        }
    }
}

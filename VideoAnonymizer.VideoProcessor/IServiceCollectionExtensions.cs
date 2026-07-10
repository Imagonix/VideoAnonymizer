using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.VideoProcessor.Analysis;
using VideoAnonymizer.VideoProcessor.Analysis.Detection;
using VideoAnonymizer.VideoProcessor.Analysis.Messaging;
using VideoAnonymizer.VideoProcessor.Analysis.Progress;
using VideoAnonymizer.VideoProcessor.Analysis.Tracking;
using VideoAnonymizer.VideoProcessor.Analysis.Tracking.Messaging;
using VideoAnonymizer.VideoProcessor.Anonymization;
using VideoAnonymizerWorker = global::VideoAnonymizer.VideoProcessor.Anonymization.VideoAnonymizer;

namespace VideoAnonymizer.VideoProcessor
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddVideoProcessorWorkers(this IServiceCollection services)
        {
            services.AddSingleton<VideoAnalysisProgressReporter>();
            services.AddSingleton<VideoAnalysisPipeline>();
            services.AddSingleton<ObjectTrackingPipeline>();
            services.AddSingletonAsHostedService<VideoAnalyzer>();
            services.AddSingletonAsHostedService<VideoAnonymizerWorker>();
            services.AddSingletonAsHostedService<SingleObjectTracker>();
            services.AddScoped<ForwardTrackingService>();
            return services;
        }

        public static IServiceCollection AddVideoProcessorMessageHandlers(this IServiceCollection services)
        {
            services.AddSingleton<IMessageHandler<AnalyzeVideo>, AnalyzeVideoHandler>();
            services.AddSingleton<IMessageHandler<AnonymizeVideo>, AnonymizeVideoHandler>();
            services.AddSingleton<IMessageHandler<TrackForwardJob>, TrackForwardVideoHandler>();
            return services;
        }

        public static IServiceCollection AddRabbitMqVideoProcessorConsumers(this IServiceCollection services)
        {
            services.AddHostedService<AnalyzeVideoConsumer>();
            services.AddHostedService<AnonymizeVideoConsumer>();
            services.AddHostedService<TrackForwardVideoConsumer>();
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

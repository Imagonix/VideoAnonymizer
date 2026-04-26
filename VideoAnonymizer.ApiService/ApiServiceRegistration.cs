using Microsoft.AspNetCore.Http.Features;
using VideoAnonymizer.ApiService.DataServices;
using VideoAnonymizer.ApiService.Notifications;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.Contracts.Messaging;

namespace VideoAnonymizer.ApiService;

public static class ApiServiceRegistration
{
    public static IMvcBuilder AddVideoAnonymizerApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();

        var mvcBuilder = builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(VideoAnonymizerApi).Assembly);

        builder.Services.AddSignalR();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddSingleton<LongRunningJobsHub>();
        builder.Services.AddScoped<VideoDataService>();
        builder.Services.AddScoped<StateDataService>();
        builder.Services.AddSingleton<IObjectDetectionApiReadyState, ObjectDetectionApiReadyState>();
        builder.Services.AddHostedService<ObjectDetectionApiStartupWaiter>();
        builder.Services.AddVideoAnonymizerNotificationHandlers();

        var objectDetectionUrl = builder.Configuration["services:objectDetection:https:0"]
            ?? builder.Configuration["ObjectDetection:BaseUrl"]
            ?? throw new InvalidOperationException("objectDetection URL not found");

        builder.Services.AddHttpClient<VideoAnonymizer.ObjectDetectionClient.ObjectDetectionClient>("objectDetection", client =>
        {
            client.BaseAddress = new Uri(objectDetectionUrl);
            client.Timeout = TimeSpan.FromSeconds(
                builder.Configuration.GetValue("ObjectDetection:RequestTimeoutSeconds", 300));
        });

        builder.Services.AddTransient(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient("objectDetection");

            var baseUrl = httpClient.BaseAddress?.ToString()
                ?? throw new InvalidOperationException("objectDetection BaseAddress is not configured.");

            return new VideoAnonymizer.ObjectDetectionClient.ObjectDetectionClient(baseUrl, httpClient);
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;
        });

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 500 * 1024 * 1024;
            options.MultipartHeadersLengthLimit = 32 * 1024;
            options.MultipartBoundaryLengthLimit = 256;
            options.MemoryBufferThreshold = 128 * 1024;
            options.ValueLengthLimit = int.MaxValue;
        });

        return mvcBuilder;
    }

    public static IServiceCollection AddVideoAnonymizerNotificationHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IMessageHandler<AnalyzedVideo>, VideoAnalyzedNotificationHandler>();
        services.AddSingleton<IMessageHandler<AnonymizedVideo>, VideoAnonymizedNotificationHandler>();
        return services;
    }

    public static IServiceCollection AddRabbitMqVideoAnonymizerNotificationConsumers(this IServiceCollection services)
    {
        services.AddHostedService<VideoAnalyzedConsumer>();
        services.AddHostedService<VideoAnonymizedConsumer>();
        return services;
    }
}

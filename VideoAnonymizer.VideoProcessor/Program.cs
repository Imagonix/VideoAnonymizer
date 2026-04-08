using MassTransit;
using VideoAnonymizer.Contracts.Extensions;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database.Extensions;
using VideoAnonymizer.ObjectDetectionClient;
using VideoAnonymizer.VideoProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingletonAsHostedService<VideoAnalyzer>();
builder.Services.AddSingletonAsHostedService<VideoAnonymizer.VideoProcessor.VideoAnonymizer>();

builder.ConfigureRabbitMQConnection();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddHostedService<AnalyzeVideoConsumer>();
builder.Services.AddHostedService<AnonymizeVideoConsumer>();

var objectDetectionUrl = builder.Configuration["services:objectDetection:https:0"]
    ?? throw new InvalidOperationException("objectDetection URL not found");

builder.Services.AddHttpClient("objectDetection", client =>
{
    client.BaseAddress = new Uri(objectDetectionUrl);
});

builder.Services.AddSingleton(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>()
        .CreateClient("objectDetection");
    var baseUrl = objectDetectionUrl;
    return new ObjectDetectionClient(baseUrl, httpClient);
});

builder.AddVideoAnonymizerDbContextFactory();

var host = builder.Build();
host.Run();

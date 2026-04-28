using VideoAnonymizer.Contracts.Extensions;
using VideoAnonymizer.Contracts.Messaging;
using VideoAnonymizer.Contracts.RabbitMQ;
using VideoAnonymizer.Database.Extensions;
using VideoAnonymizer.ObjectDetectionClient;
using VideoAnonymizer.VideoProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddVideoProcessorWorkers();
builder.Services.AddVideoProcessorMessageHandlers();

builder.ConfigureRabbitMQConnection();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();
builder.Services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
builder.Services.AddRabbitMqVideoProcessorConsumers();

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

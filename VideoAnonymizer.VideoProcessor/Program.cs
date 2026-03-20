using MassTransit;
using VideoAnonymizer.Contracts;
using VideoAnonymizer.ObjectDetectionClient;
using VideoAnonymizer.VideoProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingletonAsHostedService<VideoAnalyzer>();
builder.Services.AddSingletonAsHostedService<VideoAnonomyzer>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AnalyzeVideoConsumer>();
    x.AddConsumer<AnonomyzeVideoConsumer>();
    x.ConfigureRabbitMq(builder);
});

var objectDetectionUrl = builder.Configuration["services:objectDetection:http:0"]
    ?? throw new InvalidOperationException("objectDetection URL not found");

builder.Services.AddHttpClient("objectDetection", client =>
{
    client.BaseAddress = new Uri(objectDetectionUrl);
});

builder.Services.AddScoped<ObjectDetectionClient>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>()
        .CreateClient("objectDetection");
    var baseUrl = objectDetectionUrl;
    return new ObjectDetectionClient(baseUrl, httpClient);
});

var host = builder.Build();
host.Run();

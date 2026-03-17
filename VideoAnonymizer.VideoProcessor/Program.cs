using MassTransit;
using VideoAnonymizer.VideoProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingletonAsHostedService<VideoAnalyzer>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CalculateTrendsConsumer>();
    x.ConfigureRabbitMq(builder);
});

var host = builder.Build();
host.Run();

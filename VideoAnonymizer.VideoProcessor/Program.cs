using MassTransit;
using VideoAnonymizer.Contracts;
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

var host = builder.Build();
host.Run();

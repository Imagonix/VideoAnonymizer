using VideoAnonymizer.Database.Extensions;
using VideoAnonymizer.Database.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.AddVideoAnonymizerDbContext();

var host = builder.Build();
host.Run();

using VideoAnonymizer.Database.Extensions;
using VideoAnonymizer.Database.Postgres.Extensions;
using VideoAnonymizer.ModelDownloader;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ModelDownloadOptions>(
    builder.Configuration.GetSection("ModelDownload"));

builder.Services.AddHttpClient<ModelDownloadService>();
builder.AddPostgresVideoAnonymizerDbContextFactory();

using var host = builder.Build();

try
{
    using var scope = host.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<ModelDownloadService>();
    await service.EnsureModelExistsAsync(CancellationToken.None);
    return 0;
}
catch (Exception ex)
{
    var logger = host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Program");

    logger.LogError(ex, "Model download failed.");
    return 1;
}
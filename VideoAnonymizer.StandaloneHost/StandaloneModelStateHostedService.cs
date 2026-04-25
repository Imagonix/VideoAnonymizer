using Microsoft.EntityFrameworkCore;
using VideoAnonymizer.Database;
using VideoAnonymizer.Database.Extensions;

namespace VideoAnonymizer.StandaloneHost;

public sealed class StandaloneModelStateHostedService(
    IConfiguration configuration,
    IDbContextFactory<VideoAnonymizerDbContext> dbFactory,
    ILogger<StandaloneModelStateHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configuredPath = configuration["ObjectDetection:ModelPath"] ?? "data/models/FaceDetector.onnx";
        var modelPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var setting = await db.SystemSettings
            .FirstOrDefaultAsync(x => x.Key == SystemSettingConstants.ModelAvailable, cancellationToken);

        if (setting is null)
        {
            setting = new SystemSetting { Key = SystemSettingConstants.ModelAvailable };
            await db.SystemSettings.AddAsync(setting, cancellationToken);
        }

        var modelAvailable = File.Exists(modelPath) && new FileInfo(modelPath).Length > 0;
        setting.SetBooleanValue(modelAvailable);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Standalone model availability: {ModelAvailable} ({ModelPath})",
            modelAvailable,
            modelPath);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

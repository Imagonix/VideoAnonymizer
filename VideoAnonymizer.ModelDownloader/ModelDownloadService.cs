using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoAnonymizer.Database;
using VideoAnonymizer.Database.Extensions;

namespace VideoAnonymizer.ModelDownloader;

public class ModelDownloadService
{
    private readonly ILogger<ModelDownloadService> _logger;
    private readonly ModelDownloadOptions _options;
    private readonly HttpClient _httpClient;
    private readonly IDbContextFactory<VideoAnonymizerDbContext> _dbFactory;

    public ModelDownloadService(
        ILogger<ModelDownloadService> logger,
        IOptions<ModelDownloadOptions> options,
        HttpClient httpClient,
        IDbContextFactory<VideoAnonymizerDbContext> dbFactory)
    {
        _logger = logger;
        _options = options.Value;
        _httpClient = httpClient;
        _dbFactory = dbFactory;
    }

    public async Task EnsureModelExistsAsync(CancellationToken stoppingToken)
    {
        await using var dbContext = await _dbFactory.CreateDbContextAsync(stoppingToken);
        var modelAvailableSystemSetting = await dbContext.SystemSettings
            .FirstOrDefaultAsync(x => x.Key.Equals(SystemSettingConstants.ModelAvailable), stoppingToken);
        if (modelAvailableSystemSetting is null)
        {
            modelAvailableSystemSetting = new SystemSetting { Key = SystemSettingConstants.ModelAvailable };
            await dbContext.AddAsync(modelAvailableSystemSetting, stoppingToken);
        }
        await SaveModelAvailable(dbContext, modelAvailableSystemSetting, false, stoppingToken);

        if (string.IsNullOrWhiteSpace(_options.TargetPath))
            throw new InvalidOperationException("ModelDownload:TargetPath is missing.");

        if (string.IsNullOrWhiteSpace(_options.Url))
            throw new InvalidOperationException("ModelDownload:Url is missing.");

        var targetPath = ResolvePath(_options.TargetPath);
        var sourcePath = string.IsNullOrWhiteSpace(_options.SourceModelPath)
            ? null
            : ResolvePath(_options.SourceModelPath);

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        if (IsValidFile(targetPath))
        {
            _logger.LogInformation("Model already exists at target: {TargetPath}", targetPath);
            await SaveModelAvailable(dbContext, modelAvailableSystemSetting, true, stoppingToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(sourcePath) && IsValidFile(sourcePath))
        {
            _logger.LogInformation(
                "Model found in source path. Copying from {SourcePath} to {TargetPath}",
                sourcePath,
                targetPath);

            if (!string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourcePath, targetPath, overwrite: true);
            }

            if (!IsValidFile(targetPath))
                throw new InvalidOperationException("Copied model file is invalid.");

            await SaveModelAvailable(dbContext, modelAvailableSystemSetting, true, stoppingToken);
            return;
        }

        _logger.LogInformation("Downloading model from {Url} to {TargetPath}", _options.Url, targetPath);

        var tempPath = targetPath + ".tmp";
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        using var response = await _httpClient.GetAsync(
            _options.Url,
            HttpCompletionOption.ResponseHeadersRead,
            stoppingToken);

        response.EnsureSuccessStatusCode();

        await using (var input = await response.Content.ReadAsStreamAsync(stoppingToken))
        await using (var output = new FileStream(
            tempPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        {
            await input.CopyToAsync(output, stoppingToken);
            await output.FlushAsync(stoppingToken);
        }

        if (!IsValidFile(tempPath))
        {
            File.Delete(tempPath);
            throw new InvalidOperationException("Downloaded model file is invalid.");
        }

        File.Move(tempPath, targetPath, overwrite: true);
        await SaveModelAvailable(dbContext, modelAvailableSystemSetting, true, stoppingToken);

        _logger.LogInformation("Model downloaded successfully to {TargetPath}", targetPath);
    }

    private async Task SaveModelAvailable(
        VideoAnonymizerDbContext dbContext,
        SystemSetting modelAvailableSystemSetting,
        bool modelAvailable,
        CancellationToken cancellationToken)
    {
        modelAvailableSystemSetting.SetBooleanValue(modelAvailable);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(_options.BasePath ?? Directory.GetCurrentDirectory(), path));
    }

    private static bool IsValidFile(string path)
    {
        if (!File.Exists(path))
            return false;

        var fileInfo = new FileInfo(path);
        return fileInfo.Length > 0;
    }
}

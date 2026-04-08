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
    private readonly VideoAnonymizerDbContext _dbContext;

    public ModelDownloadService(
        ILogger<ModelDownloadService> logger,
        IOptions<ModelDownloadOptions> options,
        HttpClient httpClient,
        VideoAnonymizerDbContext dbContext)
    {
        _logger = logger;
        _options = options.Value;
        _httpClient = httpClient;
        _dbContext = dbContext;
    }

    public async Task EnsureModelExistsAsync(CancellationToken stoppingToken)
    {
        var modelAvailableSystemSetting = await _dbContext.SystemSettings.FirstOrDefaultAsync(x => x.Key.Equals(SystemSettingConstants.ModelAvailable));
        if (modelAvailableSystemSetting is null)
        {
            modelAvailableSystemSetting = new SystemSetting { Key = SystemSettingConstants.ModelAvailable };
            await _dbContext.AddAsync(modelAvailableSystemSetting);
        }
        await SaveModelAvailable(modelAvailableSystemSetting, false);

        if (string.IsNullOrWhiteSpace(_options.TargetPath))
            throw new InvalidOperationException("ModelDownload:TargetPath is missing.");

        if (string.IsNullOrWhiteSpace(_options.Url))
            throw new InvalidOperationException("ModelDownload:Url is missing.");

        var targetPath = Path.GetFullPath(_options.TargetPath);
        var sourcePath = string.IsNullOrWhiteSpace(_options.SourceModelPath)
            ? null
            : Path.GetFullPath(_options.SourceModelPath);

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        if (IsValidFile(targetPath))
        {
            _logger.LogInformation("Model already exists at target: {TargetPath}", targetPath);
            await SaveModelAvailable(modelAvailableSystemSetting, true);
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

            await SaveModelAvailable(modelAvailableSystemSetting, true);
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
        await SaveModelAvailable(modelAvailableSystemSetting, true);

        _logger.LogInformation("Model downloaded successfully to {TargetPath}", targetPath);
    }

    private async Task SaveModelAvailable(SystemSetting modelAvailableSystemSetting, bool modelAvailable)
    {
        modelAvailableSystemSetting.SetBooleanValue(modelAvailable);
        await _dbContext.SaveChangesAsync();
    }

    private static bool IsValidFile(string path)
    {
        if (!File.Exists(path))
            return false;

        var fileInfo = new FileInfo(path);
        return fileInfo.Length > 0;
    }
}
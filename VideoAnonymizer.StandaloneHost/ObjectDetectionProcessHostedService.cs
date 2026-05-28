using System.Diagnostics;

namespace VideoAnonymizer.StandaloneHost;

public sealed class ObjectDetectionProcessHostedService(
    IConfiguration configuration,
    ILogger<ObjectDetectionProcessHostedService> logger) : BackgroundService, IDisposable
{
    private Process? _process;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = configuration.GetValue("ObjectDetection:Port", 8765);
        var modelPathAnchor = ResolvePath(configuration["ObjectDetection:ModelsPath"] ?? "data/models");

        await WaitForDetectorModelsAsync(modelPathAnchor, stoppingToken);

        var configuredExecutable = configuration["ObjectDetection:ExecutablePath"] ?? "ObjectDetection/VideoAnonymizer.ObjectDetection.exe";
        var executablePath = ResolvePath(configuredExecutable);
        var workingDirectory = ResolvePath(configuration["ObjectDetection:WorkingDirectory"] ?? Path.GetDirectoryName(executablePath) ?? ".");

        var startInfo = File.Exists(executablePath)
            ? CreateExecutableStartInfo(executablePath, workingDirectory, port, modelPathAnchor)
            : CreateDevelopmentFallbackStartInfo(port, modelPathAnchor);

        startInfo.Environment["MODELS_PATH"] = modelPathAnchor;
        startInfo.Environment["PORT"] = port.ToString();
        startInfo.Environment["PYTHON_ENV"] = "Standalone";
        startInfo.Environment["PYTHONUNBUFFERED"] = "1";

        logger.LogInformation("Starting object detection service: {FileName} {Arguments}", startInfo.FileName, startInfo.Arguments);
        _process = Process.Start(startInfo);

        if (_process is null)
        {
            throw new InvalidOperationException("Object detection process could not be started.");
        }

        _ = PipeOutputAsync(_process.StandardOutput, LogLevel.Information, stoppingToken);
        _ = PipeOutputAsync(_process.StandardError, LogLevel.Warning, stoppingToken);

        await _process.WaitForExitAsync(stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (_process is null || _process.HasExited)
        {
            return base.StopAsync(cancellationToken);
        }

        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not stop object detection process.");
        }

        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _process?.Dispose();
        base.Dispose();
    }

    private async Task WaitForDetectorModelsAsync(string modelPathAnchor, CancellationToken cancellationToken)
    {
        var modelsDirectory = ResolveModelsDirectory(modelPathAnchor);

        while (!cancellationToken.IsCancellationRequested)
        {
            var modelPath = FindFirstValidDetectorModel(modelsDirectory);
            if (modelPath is not null)
            {
                logger.LogInformation(
                    "Object detection models are available in {ModelsDirectory}. Found detector model {DetectorModelPath}.",
                    modelsDirectory,
                    modelPath);
                return;
            }

            logger.LogInformation(
                "No valid object detector model/config pair is available yet in {ModelsDirectory}. Waiting for bundled or user-provided model files.",
                modelsDirectory);

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private static ProcessStartInfo CreateExecutableStartInfo(
        string executablePath,
        string workingDirectory,
        int port,
        string modelPath)
    {
        return new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = $"--host 127.0.0.1 --port {port} --models-path \"{modelPath}\"",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    private static ProcessStartInfo CreateDevelopmentFallbackStartInfo(int port, string modelPath)
    {
        var objectDetectionSource = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "VideoAnonymizer.ObjectDetection"));

        return new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"-m uvicorn main:app --host 127.0.0.1 --port {port}",
            WorkingDirectory = objectDetectionSource,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    private async Task PipeOutputAsync(StreamReader reader, LogLevel logLevel, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    return;
                }

                logger.Log(logLevel, "ObjectDetection: {Line}", line);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Object detection output stream ended.");
        }
    }

    private static string ResolvePath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
    }

    private static string ResolveModelsDirectory(string modelPathAnchor)
    {
        return string.Equals(Path.GetExtension(modelPathAnchor), ".onnx", StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(modelPathAnchor) ?? modelPathAnchor
            : modelPathAnchor;
    }

    private static string? FindFirstValidDetectorModel(string modelsDirectory)
    {
        if (!Directory.Exists(modelsDirectory))
        {
            return null;
        }

        return Directory.EnumerateFiles(modelsDirectory, "*.onnx")
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(modelPath =>
            {
                var configPath = Path.ChangeExtension(modelPath, ".detector.json");
                return IsValidModelFile(modelPath) && IsValidModelFile(configPath);
            });
    }

    private static bool IsValidModelFile(string modelPath)
    {
        return File.Exists(modelPath) && new FileInfo(modelPath).Length > 0;
    }
}

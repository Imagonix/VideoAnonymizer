using System.Diagnostics;

namespace VideoAnonymizer.StandaloneHost;

public sealed class ObjectDetectionProcessHostedService(
    IConfiguration configuration,
    ILogger<ObjectDetectionProcessHostedService> logger) : IHostedService, IDisposable
{
    private Process? _process;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var port = configuration.GetValue("ObjectDetection:Port", 8765);
        var modelPath = ResolvePath(configuration["ObjectDetection:ModelPath"] ?? "data/models/FaceDetector.onnx");
        if (!File.Exists(modelPath) || new FileInfo(modelPath).Length == 0)
        {
            logger.LogWarning(
                "Object detection model was not found at {ModelPath}. Object detection service will not be started.",
                modelPath);
            return Task.CompletedTask;
        }

        var configuredExecutable = configuration["ObjectDetection:ExecutablePath"] ?? "ObjectDetection/VideoAnonymizer.ObjectDetection.exe";
        var executablePath = ResolvePath(configuredExecutable);
        var workingDirectory = ResolvePath(configuration["ObjectDetection:WorkingDirectory"] ?? Path.GetDirectoryName(executablePath) ?? ".");

        var startInfo = File.Exists(executablePath)
            ? CreateExecutableStartInfo(executablePath, workingDirectory, port, modelPath)
            : CreateDevelopmentFallbackStartInfo(port, modelPath);

        startInfo.Environment["FACE_DETECTOR_MODEL_PATH"] = modelPath;
        startInfo.Environment["PORT"] = port.ToString();
        startInfo.Environment["PYTHON_ENV"] = "Standalone";
        startInfo.Environment["PYTHONUNBUFFERED"] = "1";

        logger.LogInformation("Starting object detection service: {FileName} {Arguments}", startInfo.FileName, startInfo.Arguments);
        _process = Process.Start(startInfo);

        if (_process is null)
        {
            throw new InvalidOperationException("Object detection process could not be started.");
        }

        _ = PipeOutputAsync(_process.StandardOutput, LogLevel.Information, cancellationToken);
        _ = PipeOutputAsync(_process.StandardError, LogLevel.Warning, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_process is null || _process.HasExited)
        {
            return Task.CompletedTask;
        }

        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not stop object detection process.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _process?.Dispose();
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
            Arguments = $"--host 127.0.0.1 --port {port} --model-path \"{modelPath}\"",
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
}

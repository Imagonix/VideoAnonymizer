using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace VideoAnonymizer.VideoProcessor;

public abstract class SingleJobQueingWorker<T> : BackgroundService
{
    private readonly Channel<T> _queue = Channel.CreateUnbounded<T>();
    private readonly ILogger _logger;

    protected SingleJobQueingWorker(ILogger logger)
    {
        _logger = logger;
    }

    public async Task EnqueueAsync(T job, CancellationToken ct = default)
    {
        await _queue.Writer.WriteAsync(job, ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SingleJobQueingWorker<{JobType}> started", typeof(T).Name);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await HandleJob(job, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling job of type {JobType}", typeof(T).Name);

                        try
                        {
                            _logger.LogDebug("Failed job content: {Job}",
                                System.Text.Json.JsonSerializer.Serialize(job));
                        }
                        catch { }
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in SingleJobQueingWorker<{JobType}>", typeof(T).Name);
            throw;
        }
        finally
        {
            _logger.LogInformation("SingleJobQueingWorker<{JobType}> stopped", typeof(T).Name);
        }
    }

    protected abstract Task HandleJob(T job, CancellationToken stoppingToken);
}
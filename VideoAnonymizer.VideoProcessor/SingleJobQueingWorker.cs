using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace VideoAnonymizer.VideoProcessor
{
    public abstract class SingleJobQueingWorker<T>(ILogger<SingleJobQueingWorker<T>> logger) : BackgroundService
    {
        protected readonly Channel<T> _queue = Channel.CreateUnbounded<T>();

        public async Task EnqueueAsync(T job, CancellationToken ct = default)
        {
            await _queue.Writer.WriteAsync(job, ct).AsTask();
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                T? currentJob;
                if (!_queue.Reader.TryRead(out currentJob) || currentJob is null)
                {
                    await Task.Delay(200);
                }
                else
                {
                    try
                    {
                        await HandleJob(currentJob, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(JsonSerializer.Serialize(currentJob));
                        logger.LogError($"Error handling job: {ex.Message}");
                    }
                }
            }
        }

        protected abstract Task HandleJob(T job, CancellationToken stoppingToken);
    }
}

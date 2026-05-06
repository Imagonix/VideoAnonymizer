using System.Threading.Channels;

namespace VideoAnonymizer.Web.Components.ReviewExport;

public sealed class VideoEditorActionQueue : IAsyncDisposable
{
    private readonly Channel<Func<Task>> _operations = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions
    {
        SingleReader = true
    });
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Func<bool, Task> _processingChanged;
    private readonly Task _worker;
    private int _pendingCount;

    public VideoEditorActionQueue(Func<bool, Task> processingChanged)
    {
        _processingChanged = processingChanged;
        _worker = ProcessQueueAsync();
    }

    public bool HasPendingActions => Volatile.Read(ref _pendingCount) > 0;

    public void Enqueue(Func<Task> operation)
    {
        Interlocked.Increment(ref _pendingCount);
        _ = NotifyProcessingChangedAsync();
        _operations.Writer.TryWrite(operation);
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _operations.Reader;

        try
        {
            while (await reader.WaitToReadAsync(_cancellationTokenSource.Token))
            {
                while (reader.TryRead(out var operation))
                {
                    try
                    {
                        await operation();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _pendingCount);
                        await NotifyProcessingChangedAsync();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Task NotifyProcessingChangedAsync() =>
        _processingChanged(HasPendingActions);

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();
        _operations.Writer.TryComplete();

        try
        {
            await _worker;
        }
        catch
        {
        }

        _cancellationTokenSource.Dispose();
    }
}

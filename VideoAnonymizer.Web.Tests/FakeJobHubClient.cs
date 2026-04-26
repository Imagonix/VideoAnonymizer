using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Services;

namespace VideoAnonymizer.Web.Tests.TestDoubles;

public sealed class FakeJobHubClient : IJobHubClient
{
    private Func<LongRunningJobFinishedMessage, Task>? _videoAnalyzedHandler;
    private Func<LongRunningJobFinishedMessage, Task>? _videoAnonymizedHandler;
    private Func<LongRunningJobProgressMessage, Task>? _jobProgressHandler;

    public bool StartCalled { get; private set; }
    public bool StopCalled { get; private set; }

    public int VideoAnalyzedSubscriptionCount { get; private set; }
    public int VideoAnonymizedSubscriptionCount { get; private set; }
    public int JobProgressSubscriptionCount { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        StartCalled = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopCalled = true;
        return Task.CompletedTask;
    }

    public IDisposable OnVideoAnalyzed(Func<LongRunningJobFinishedMessage, Task> handler)
    {
        VideoAnalyzedSubscriptionCount++;
        _videoAnalyzedHandler = handler;
        return new CallbackDisposable(() => _videoAnalyzedHandler = null);
    }

    public IDisposable OnVideoAnonymized(Func<LongRunningJobFinishedMessage, Task> handler)
    {
        VideoAnonymizedSubscriptionCount++;
        _videoAnonymizedHandler = handler;
        return new CallbackDisposable(() => _videoAnonymizedHandler = null);
    }

    public IDisposable OnJobProgress(Func<LongRunningJobProgressMessage, Task> handler)
    {
        JobProgressSubscriptionCount++;
        _jobProgressHandler = handler;
        return new CallbackDisposable(() => _jobProgressHandler = null);
    }

    public async Task RaiseVideoAnalyzedAsync(LongRunningJobFinishedMessage message)
    {
        if (_videoAnalyzedHandler is not null)
        {
            await _videoAnalyzedHandler(message);
        }
    }

    public async Task RaiseVideoAnonymizedAsync(LongRunningJobFinishedMessage message)
    {
        if (_videoAnonymizedHandler is not null)
        {
            await _videoAnonymizedHandler(message);
        }
    }

    public async Task RaiseJobProgressAsync(LongRunningJobProgressMessage message)
    {
        if (_jobProgressHandler is not null)
        {
            await _jobProgressHandler(message);
        }
    }

    private sealed class CallbackDisposable : IDisposable
    {
        private readonly Action _dispose;

        public CallbackDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose();
        }
    }
}

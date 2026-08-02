using VideoAnonymizer.Web.Shared;
using VideoAnonymizer.Web.Services;

namespace VideoAnonymizer.Web.Tests.TestDoubles;

public sealed class FakeJobHubClient : IJobHubClient
{
    private Func<LongRunningJobFinishedMessage, Task>? _videoAnalyzedHandler;
    private Func<LongRunningJobFinishedMessage, Task>? _videoAnonymizedHandler;
    private Func<TrackForwardCompletedMessage, Task>? _trackForwardCompletedHandler;
    private Func<TrackForwardProgressMessage, Task>? _trackForwardProgressHandler;
    private Func<LongRunningJobProgressMessage, Task>? _jobProgressHandler;

    public bool StartCalled { get; private set; }
    public bool StopCalled { get; private set; }

    public int VideoAnalyzedSubscriptionCount { get; private set; }
    public int VideoAnonymizedSubscriptionCount { get; private set; }
    public int TrackForwardCompletedSubscriptionCount { get; private set; }
    public int TrackForwardProgressSubscriptionCount { get; private set; }
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

    public IDisposable OnTrackForwardCompleted(Func<TrackForwardCompletedMessage, Task> handler)
    {
        TrackForwardCompletedSubscriptionCount++;
        _trackForwardCompletedHandler = handler;
        return new CallbackDisposable(() => _trackForwardCompletedHandler = null);
    }

    public IDisposable OnTrackForwardProgress(Func<TrackForwardProgressMessage, Task> handler)
    {
        TrackForwardProgressSubscriptionCount++;
        _trackForwardProgressHandler = handler;
        return new CallbackDisposable(() => _trackForwardProgressHandler = null);
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

    public async Task RaiseTrackForwardCompletedAsync(TrackForwardCompletedMessage message)
    {
        if (_trackForwardCompletedHandler is not null)
        {
            await _trackForwardCompletedHandler(message);
        }
    }

    public async Task RaiseTrackForwardProgressAsync(TrackForwardProgressMessage message)
    {
        if (_trackForwardProgressHandler is not null)
        {
            await _trackForwardProgressHandler(message);
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

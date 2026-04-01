using VideoAnonymizer.Web.Contracts;

namespace VideoAnonymizer.Web.Services;

public interface IJobHubClient
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);

    IDisposable OnVideoAnalyzed(Func<LongRunningJobFinishedMessage, Task> handler);
    IDisposable OnVideoAnonymized(Func<LongRunningJobFinishedMessage, Task> handler);
}
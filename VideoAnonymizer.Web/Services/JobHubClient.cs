using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using VideoAnonymizer.Web.Shared;

namespace VideoAnonymizer.Web.Services;

public sealed class JobHubClient : IJobHubClient, IAsyncDisposable
{
    private readonly HubConnection _hubConnection;

    public JobHubClient(IConfiguration configuration)
    {
        var hubUrl = $"{configuration.GetApiServiceBaseUrl()}{SharedConstants.SignalR.JobHubUrl}";
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
        => _hubConnection.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _hubConnection.StopAsync(cancellationToken);

    public IDisposable OnVideoAnalyzed(Func<LongRunningJobFinishedMessage, Task> handler)
        => _hubConnection.On(SharedConstants.SignalR.Messages.VideoAnalyzed, handler);

    public IDisposable OnVideoAnonymized(Func<LongRunningJobFinishedMessage, Task> handler)
        => _hubConnection.On(SharedConstants.SignalR.Messages.VideoAnonymized, handler);

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
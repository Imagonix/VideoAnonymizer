using System.Threading.Channels;

namespace VideoAnonymizer.ApiService.DataServices;

public sealed class TrackForwardJobQueue
{
    private readonly Channel<TrackForwardJob> _jobs = Channel.CreateUnbounded<TrackForwardJob>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(TrackForwardJob job, CancellationToken cancellationToken = default) =>
        _jobs.Writer.WriteAsync(job, cancellationToken);

    public IAsyncEnumerable<TrackForwardJob> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _jobs.Reader.ReadAllAsync(cancellationToken);
}

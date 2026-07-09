using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Utilix.Abstractions.Jobs;

namespace Utilix.Api.Infrastructure.Queue;

public sealed class ChannelJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = false,
        SingleWriter = false
    });

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(jobId, cancellationToken);

    public async IAsyncEnumerable<Guid> DequeueAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var jobId in _channel.Reader.ReadAllAsync(cancellationToken))
            yield return jobId;
    }
}

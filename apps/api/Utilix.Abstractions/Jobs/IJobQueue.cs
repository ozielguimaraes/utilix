namespace Utilix.Abstractions.Jobs;

public interface IJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}

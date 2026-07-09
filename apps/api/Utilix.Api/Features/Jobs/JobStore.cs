using System.Collections.Concurrent;

namespace Utilix.Api.Features.Jobs;

public sealed class JobStore
{
    private readonly ConcurrentDictionary<Guid, JobRecord> _jobs = new();

    public JobRecord Add(JobRecord job)
    {
        _jobs[job.Id] = job;
        return job;
    }

    public JobRecord? Get(Guid id) => _jobs.TryGetValue(id, out var job) ? job : null;

    public void Update(Guid id, Action<JobRecord> mutate)
    {
        if (_jobs.TryGetValue(id, out var job))
        {
            lock (job)
            {
                mutate(job);
            }
        }
    }

    public IEnumerable<JobRecord> GetAll() => _jobs.Values;
}

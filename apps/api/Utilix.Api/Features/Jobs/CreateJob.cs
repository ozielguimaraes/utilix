using Utilix.Abstractions.Jobs;
using Utilix.Api.Features.Jobs;

namespace Utilix.Api.Features.Jobs;

public sealed record CreateJobRequest(string EngineSlug, string? Url, Dictionary<string, string>? Options);
public sealed record CreateJobResponse(Guid JobId, string Status, DateTimeOffset CreatedAt);

public static class CreateJob
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handler)
            .WithName("CreateJob");

    private static async Task<IResult> Handler(
        CreateJobRequest request,
        JobStore jobStore,
        IJobQueue jobQueue,
        CancellationToken cancellationToken,
        ILogger<JobStore> logger)
    {
        if (string.IsNullOrWhiteSpace(request.EngineSlug))
            return Results.BadRequest(new { error = "engineSlug é obrigatório" });

        var jobId = Guid.NewGuid();
        var job = new JobRecord
        {
            Id = jobId,
            EngineSlug = request.EngineSlug,
            Url = request.Url ?? "",
            Options = request.Options ?? new Dictionary<string, string>(),
        };

        jobStore.Add(job);
        await jobQueue.EnqueueAsync(jobId, cancellationToken);

        logger.LogInformation("Job criado: {JobId} com engine {Engine}", jobId, request.EngineSlug);

        return Results.Accepted(
            $"/api/jobs/{jobId}",
            new CreateJobResponse(jobId, "pending", job.CreatedAt));
    }
}

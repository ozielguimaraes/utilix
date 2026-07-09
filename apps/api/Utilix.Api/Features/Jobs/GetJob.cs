using Utilix.Api.Features.Jobs;

namespace Utilix.Api.Features.Jobs;

public sealed record GetJobResponse(
    Guid JobId,
    string Status,
    ProgressReportDto? Progress,
    JobResultInfoDto? Result,
    JobErrorInfoDto? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record ProgressReportDto(int Percent, string Stage, string? Message);
public sealed record JobResultInfoDto(string FileName, string DownloadUrl, long SizeBytes, DateTime ExpiresAt);
public sealed record JobErrorInfoDto(string MessageKey, bool Retryable);

public static class GetJob
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{jobId}", Handler)
            .WithName("GetJob");

    private static IResult Handler(
        Guid jobId,
        JobStore jobStore)
    {
        var job = jobStore.Get(jobId);
        if (job is null)
            return Results.NotFound();

        var response = new GetJobResponse(
            job.Id,
            job.Status.ToString().ToLowerInvariant(),
            job.Progress is not null
                ? new ProgressReportDto(job.Progress.Percent, job.Progress.Stage, job.Progress.Message)
                : null,
            job.Result is not null
                ? new JobResultInfoDto(job.Result.FileName, job.Result.DownloadUrl, job.Result.SizeBytes, job.Result.ExpiresAt)
                : null,
            job.Error is not null
                ? new JobErrorInfoDto(job.Error.MessageKey, job.Error.Retryable)
                : null,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt);

        return Results.Ok(response);
    }
}

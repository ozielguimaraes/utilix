using Utilix.Abstractions.Engines;

namespace Utilix.Api.Features.Jobs;

public enum JobStatus { Pending, Processing, Completed, Failed, Cancelled }

public record JobResultInfo(string FileName, string DownloadUrl, long SizeBytes, DateTime ExpiresAt);
public record JobErrorInfo(string MessageKey, bool Retryable);

public sealed class JobRecord
{
    public required Guid Id { get; init; }
    public required string EngineSlug { get; init; }
    public required string Url { get; init; }
    public required IReadOnlyDictionary<string, string> Options { get; init; }

    public JobStatus Status { get; set; } = JobStatus.Pending;
    public ProgressReport? Progress { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public JobResultInfo? Result { get; set; }
    public string? StorageKey { get; set; }
    public JobErrorInfo? Error { get; set; }
}

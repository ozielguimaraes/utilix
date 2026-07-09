using Utilix.Abstractions.Engines;
using Utilix.Abstractions.Jobs;
using Utilix.Abstractions.Storage;
using Utilix.Api.Engines;
using Utilix.Api.Features.Jobs;

namespace Utilix.Api.Infrastructure.Queue;

public sealed class JobOrchestrator(
    IJobQueue jobQueue,
    JobStore jobStore,
    EngineRegistry engineRegistry,
    IFileStorage fileStorage,
    ILogger<JobOrchestrator> logger) : BackgroundService
{
    private const int JobTimeoutSeconds = 600;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("JobOrchestrator iniciado");

        try
        {
            await foreach (var jobId in jobQueue.DequeueAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessJobAsync(jobId, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao processar job {JobId}", jobId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("JobOrchestrator parado");
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken stoppingToken)
    {
        var job = jobStore.Get(jobId);
        if (job is null)
        {
            logger.LogWarning("Job não encontrado: {JobId}", jobId);
            return;
        }

        jobStore.Update(jobId, j =>
        {
            j.Status = JobStatus.Processing;
            j.StartedAt = DateTimeOffset.UtcNow;
        });

        var engine = engineRegistry.GetEngine(job.EngineSlug);
        if (engine is null)
        {
            FailJob(jobId, "errors.engine.not_found", false);
            return;
        }

        var workingDir = Path.Combine(Path.GetTempPath(), "utilix", jobId.ToString());

        try
        {
            Directory.CreateDirectory(workingDir);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(JobTimeoutSeconds * 1000);

            var input = new ConversionInput(
                jobId,
                job.EngineSlug,
                null,
                null,
                job.Url,
                job.Options,
                workingDir);

            var progress = new AsyncProgress<ProgressReport>(report =>
            {
                jobStore.Update(jobId, j => j.Progress = report);
                return Task.CompletedTask;
            });

            logger.LogInformation("Iniciando job {JobId} com engine {Engine}", jobId, job.EngineSlug);
            var result = await engine.ExecuteAsync(input, progress, cts.Token);

            var storageKey = $"{jobId}/{result.OutputFileName}";
            var fileSize = await fileStorage.SaveAsync(storageKey, result.OutputPath, stoppingToken);

            jobStore.Update(jobId, j =>
            {
                j.Status = JobStatus.Completed;
                j.CompletedAt = DateTimeOffset.UtcNow;
                j.StorageKey = storageKey;
                j.Result = new JobResultInfo(
                    result.OutputFileName,
                    $"/api/jobs/{jobId}/download",
                    fileSize,
                    DateTime.UtcNow.AddHours(1));
            });

            logger.LogInformation("Job {JobId} concluído com sucesso", jobId);
        }
        catch (EngineExecutionException ex)
        {
            FailJob(jobId, ex.UserMessageKey, true);
        }
        catch (OperationCanceledException)
        {
            FailJob(jobId, "errors.job.timeout", false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no job {JobId}", jobId);
            FailJob(jobId, "errors.job.unknown_error", true);
        }
        finally
        {
            try
            {
                if (Directory.Exists(workingDir))
                    Directory.Delete(workingDir, recursive: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao limpar diretório de trabalho {WorkingDir}", workingDir);
            }
        }
    }

    private void FailJob(Guid jobId, string messageKey, bool retryable)
    {
        jobStore.Update(jobId, j =>
        {
            j.Status = JobStatus.Failed;
            j.CompletedAt = DateTimeOffset.UtcNow;
            j.Error = new JobErrorInfo(messageKey, retryable);
        });
    }
}

/// <summary>
/// Wrapper que transforma um <see cref="IProgress{T}"/> síncrono em assíncrono,
/// permitindo que o Report dispare tasks sem bloquear o caller.
/// </summary>
internal sealed class AsyncProgress<T>(Func<T, Task> handler) : IProgress<T>
{
    public void Report(T value) => _ = handler(value);
}

internal static class AsyncProgressExtensions
{
    public static AsyncProgress<T> Create<T>(Action<T> handler) =>
        new(report => { handler(report); return Task.CompletedTask; });
}

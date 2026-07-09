using Utilix.Abstractions.Storage;
using Utilix.Api.Features.Jobs;

namespace Utilix.Api.Features.Jobs;

public static class DownloadJob
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{jobId}/download", Handler)
            .WithName("DownloadJob");

    private static async Task<IResult> Handler(
        Guid jobId,
        JobStore jobStore,
        IFileStorage fileStorage,
        CancellationToken cancellationToken,
        ILogger<JobStore> logger)
    {
        var job = jobStore.Get(jobId);
        if (job is null)
            return Results.NotFound();

        if (job.StorageKey is null || job.Result is null)
            return Results.BadRequest(new { error = "Job não possui resultado disponível" });

        try
        {
            var (stream, contentType) = await fileStorage.OpenReadAsync(job.StorageKey, cancellationToken);
            logger.LogInformation("Download iniciado: {JobId} ({FileName})", jobId, job.Result.FileName);

            return Results.File(stream, contentType, job.Result.FileName);
        }
        catch (FileNotFoundException)
        {
            logger.LogWarning("Arquivo não encontrado no storage: {StorageKey}", job.StorageKey);
            return Results.NotFound();
        }
    }
}

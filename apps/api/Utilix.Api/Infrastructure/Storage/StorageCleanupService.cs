using Utilix.Abstractions.Storage;

namespace Utilix.Api.Infrastructure.Storage;

public sealed class StorageCleanupService(IFileStorage storage, ILogger<StorageCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan FileRetention = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StorageCleanupService iniciado");

        using var timer = new PeriodicTimer(CleanupInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CleanupExpiredFilesAsync(stoppingToken);
            }
        }
        finally
        {
            timer.Dispose();
            logger.LogInformation("StorageCleanupService parado");
        }
    }

    private async Task CleanupExpiredFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cutoffTime = DateTimeOffset.UtcNow - FileRetention;
            var deletedCount = 0;

            await foreach (var file in storage.ListAsync(cancellationToken))
            {
                if (file.LastModifiedUtc < cutoffTime)
                {
                    await storage.DeleteAsync(file.Key, cancellationToken);
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
                logger.LogInformation("Limpeza de storage: {Count} arquivos deletados", deletedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante limpeza de storage");
        }
    }
}

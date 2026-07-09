namespace Utilix.Abstractions.Storage;

public record StorageObject(string Key, DateTimeOffset LastModifiedUtc, long SizeBytes);

public interface IFileStorage
{
    Task<long> SaveAsync(string key, string sourceFilePath, CancellationToken cancellationToken);
    Task<(Stream Stream, string ContentType)> OpenReadAsync(string key, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
    IAsyncEnumerable<StorageObject> ListAsync(CancellationToken cancellationToken);
}

using System.Runtime.CompilerServices;
using Utilix.Abstractions.Storage;

namespace Utilix.Api.Infrastructure.Storage;

public sealed class LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger) : IFileStorage
{
    private readonly string _rootPath = configuration["Storage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "data", "outputs");

    public async Task<long> SaveAsync(string key, string sourceFilePath, CancellationToken cancellationToken)
    {
        var destPath = Path.Combine(_rootPath, key);
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Arquivo de origem não encontrado: {sourceFilePath}");

        using (var source = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write))
        {
            await source.CopyToAsync(dest, cancellationToken);
        }

        var fileInfo = new FileInfo(destPath);
        logger.LogInformation("Arquivo salvo: {Key} ({SizeBytes} bytes)", key, fileInfo.Length);
        return fileInfo.Length;
    }

    public async Task<(Stream Stream, string ContentType)> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_rootPath, key);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Arquivo não encontrado: {path}");

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var contentType = GetContentType(path);
        return (stream, contentType);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        return Task.FromResult(File.Exists(Path.Combine(_rootPath, key)));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_rootPath, key);
        if (File.Exists(path))
        {
            File.Delete(path);
            logger.LogInformation("Arquivo deletado: {Key}", key);
        }
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<StorageObject> ListAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_rootPath))
            yield break;

        foreach (var file in Directory.EnumerateFiles(_rootPath, "*", SearchOption.AllDirectories))
        {
            var info = new FileInfo(file);
            var key = Path.GetRelativePath(_rootPath, file);
            yield return new StorageObject(key, info.LastWriteTimeUtc, info.Length);
            await Task.Yield();
        }
    }

    private static string GetContentType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".mp3" => "audio/mpeg",
        ".webm" => "video/webm",
        ".mov" => "video/quicktime",
        ".m4a" => "audio/mp4",
        ".wav" => "audio/wav",
        _ => "application/octet-stream",
    };
}

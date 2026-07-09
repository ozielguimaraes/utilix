namespace Utilix.Abstractions.Engines;

public record ConversionInput(
    Guid JobId,
    string EngineSlug,
    Stream? SourceStream,
    string? SourceFileName,
    string? Url,
    IReadOnlyDictionary<string, string> Options,
    string WorkingDirectory);

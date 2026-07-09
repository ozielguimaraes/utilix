namespace Utilix.Abstractions.Engines;

public record EngineMetadata(
    string Slug,
    string DisplayNameKey,
    string Category,
    string[] AcceptedInputs,
    string[] OutputFormats,
    EngineOption[] Options,
    long MaxInputSizeBytes);

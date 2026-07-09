namespace Utilix.Abstractions.Engines;

public interface IConversionEngine
{
    EngineMetadata Metadata { get; }

    Task<ConversionResult> ExecuteAsync(
        ConversionInput input,
        IProgress<ProgressReport> progress,
        CancellationToken cancellationToken);
}

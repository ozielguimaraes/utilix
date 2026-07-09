using Utilix.Abstractions.Engines;

namespace Utilix.Api.Engines;

public sealed class EngineRegistry(IEnumerable<IConversionEngine> engines)
{
    private readonly IReadOnlyDictionary<string, IConversionEngine> _enginesBySlug =
        engines.ToDictionary(e => e.Metadata.Slug, StringComparer.OrdinalIgnoreCase);

    public IConversionEngine? GetEngine(string slug) =>
        _enginesBySlug.GetValueOrDefault(slug, null);

    public IEnumerable<EngineMetadata> GetAllMetadata() =>
        _enginesBySlug.Values.Select(e => e.Metadata);
}

using Utilix.Api.Engines;

namespace Utilix.Api.Features.Engines;

public sealed record EngineMetadataDto(
    string Slug,
    string DisplayNameKey,
    string Category,
    string[] AcceptedInputs,
    string[] OutputFormats,
    EngineOptionDto[] Options,
    long MaxInputSizeBytes);

public sealed record EngineOptionDto(string Key, string Type, string DefaultValue, string[] Choices);

public static class ListEngines
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handler)
            .WithName("ListEngines");

    private static IResult Handler(EngineRegistry engineRegistry)
    {
        var engines = engineRegistry.GetAllMetadata()
            .Select(m => new EngineMetadataDto(
                m.Slug,
                m.DisplayNameKey,
                m.Category,
                m.AcceptedInputs,
                m.OutputFormats,
                m.Options.Select(o => new EngineOptionDto(o.Key, o.Type, o.DefaultValue, o.Choices)).ToArray(),
                m.MaxInputSizeBytes))
            .ToList();

        return Results.Ok(engines);
    }
}

namespace Utilix.Abstractions.Engines;

public record EngineOption(
    string Key,
    string Type,
    string DefaultValue,
    string[] Choices);

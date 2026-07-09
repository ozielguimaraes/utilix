namespace Utilix.Abstractions.Engines;

public record ProgressReport(
    int Percent,
    string Stage,
    string? Message);

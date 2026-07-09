namespace Utilix.Abstractions.Engines;

public record ConversionResult(
    string OutputPath,
    string OutputFileName,
    string MimeType,
    long SizeBytes);

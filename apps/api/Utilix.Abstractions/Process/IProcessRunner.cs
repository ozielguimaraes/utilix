namespace Utilix.Abstractions.Process;

public record ProcessRunResult(int ExitCode, TimeSpan Duration);

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        Action<string> onOutputLine,
        Action<string> onErrorLine,
        CancellationToken cancellationToken);
}

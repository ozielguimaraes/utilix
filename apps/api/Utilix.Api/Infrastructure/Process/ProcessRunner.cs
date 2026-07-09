using System.Diagnostics;
using Utilix.Abstractions.Process;

namespace Utilix.Api.Infrastructure.Process;

public sealed class ProcessRunner(ILogger<ProcessRunner> logger) : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        Action<string> onOutputLine,
        Action<string> onErrorLine,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        var stopwatch = Stopwatch.StartNew();
        using var process = new System.Diagnostics.Process { StartInfo = psi };

        var outputTask = process.StandardOutput.ReadLineAsync();
        var errorTask = process.StandardError.ReadLineAsync();

        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"Falha ao iniciar processo: {fileName}");

            var _ = "processo iniciado";

            logger.LogInformation("Processo iniciado: {FileName} com {ArgCount} argumentos", fileName, arguments.Count);

            var outputTask_handle = Task.Run(async () =>
            {
                var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
                while (line != null)
                {
                    onOutputLine(line);
                    line = await process.StandardOutput.ReadLineAsync(cancellationToken);
                }
            }, cancellationToken);

            var errorTask_handle = Task.Run(async () =>
            {
                var line = await process.StandardError.ReadLineAsync(cancellationToken);
                while (line != null)
                {
                    onErrorLine(line);
                    line = await process.StandardError.ReadLineAsync(cancellationToken);
                }
            }, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new OperationCanceledException("Processo cancelado pelo token.");
            }

            await Task.WhenAll(outputTask_handle, errorTask_handle);

            stopwatch.Stop();
            logger.LogInformation("Processo finalizado: {FileName} com exit code {ExitCode} ({Duration}ms)",
                fileName, process.ExitCode, stopwatch.ElapsedMilliseconds);

            return new ProcessRunResult(process.ExitCode, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao executar processo: {FileName}", fileName);
            throw;
        }
    }
}

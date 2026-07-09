using Utilix.Abstractions.Process;

namespace Utilix.Tests.Infrastructure;

public sealed class FakeProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        Action<string> onOutputLine,
        Action<string> onErrorLine,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(workingDirectory);

        var fileName_lower = Path.GetFileName(fileName).ToLowerInvariant();
        string outputFile = fileName_lower.Contains("yt-dlp")
            ? Path.Combine(workingDirectory, "fake_video.mp4")
            : Path.Combine(workingDirectory, "fake_output.tmp");

        bool isAudio = arguments.Contains("-x");
        if (isAudio)
            outputFile = Path.Combine(workingDirectory, "fake_audio.mp3");

        File.WriteAllText(outputFile, "FAKE_MEDIA_DATA");

        for (var i = 0; i <= 100; i += 25)
        {
            onErrorLine($"[download] {i}.0% of ~5.00MiB");
            await Task.Delay(10, cancellationToken);
        }

        onOutputLine(outputFile);

        return new ProcessRunResult(0, TimeSpan.FromMilliseconds(100));
    }
}

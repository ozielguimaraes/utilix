using System.Text.RegularExpressions;
using Utilix.Abstractions.Engines;
using Utilix.Abstractions.Process;

namespace Utilix.Api.Engines;

public sealed class YoutubeEngine(
    IProcessRunner processRunner,
    IConfiguration configuration,
    ILogger<YoutubeEngine> logger) : IConversionEngine
{
    private static readonly string[] AllowedHosts =
    [
        "youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtu.be", "www.youtu.be"
    ];

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";

    private static readonly Regex ProgressRegex = new(@"\[download\]\s+(\d{1,3}(?:\.\d+)?)%", RegexOptions.Compiled);

    public EngineMetadata Metadata => new(
        Slug: "youtube",
        DisplayNameKey: "engines.youtube.name",
        Category: "media",
        AcceptedInputs: ["url"],
        OutputFormats: ["mp4", "mp3"],
        Options:
        [
            new EngineOption("format", "select", "video", ["video", "audio"]),
            new EngineOption("quality", "select", "best", ["best", "1080p", "720p", "480p"]),
        ],
        MaxInputSizeBytes: 0);

    public async Task<ConversionResult> ExecuteAsync(
        ConversionInput input,
        IProgress<ProgressReport> progress,
        CancellationToken cancellationToken)
    {
        ValidateUrl(input.Url);

        var format = input.Options.GetValueOrDefault("format", "video");
        var quality = input.Options.GetValueOrDefault("quality", "best");
        var args = BuildArgs(input.Url!, format, quality);

        progress.Report(new(0, "starting", null));

        string? finalPath = null;
        var stderrTail = new List<string>();

        var result = await processRunner.RunAsync(
            configuration["Youtube:YtDlpPath"] ?? "yt-dlp",
            args,
            input.WorkingDirectory,
            onOutputLine: line =>
            {
                if (!string.IsNullOrEmpty(line) && (line.StartsWith("/") || line.Contains(':')))
                    finalPath = line.Trim();
            },
            onErrorLine: line =>
            {
                stderrTail.Add(line);
                if (stderrTail.Count > 10)
                    stderrTail.RemoveAt(0);

                var match = ProgressRegex.Match(line);
                if (match.Success && double.TryParse(match.Groups[1].Value, out var pct))
                    progress.Report(new((int)pct, "downloading", null));
            },
            cancellationToken);

        if (result.ExitCode != 0 || finalPath is null || !File.Exists(finalPath))
        {
            var errorMsg = string.Join('\n', stderrTail.TakeLast(5));
            logger.LogError("yt-dlp falhou com exit code {ExitCode}: {Error}", result.ExitCode, errorMsg);
            throw new EngineExecutionException(
                $"yt-dlp saiu com código {result.ExitCode}",
                userMessageKey: "errors.engine.youtube_failed");
        }

        progress.Report(new(100, "finalizing", null));

        var mime = format == "audio" ? "audio/mpeg" : "video/mp4";
        var fileInfo = new FileInfo(finalPath);

        return new ConversionResult(
            finalPath,
            fileInfo.Name,
            mime,
            fileInfo.Length);
    }

    private static void ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new EngineExecutionException("URL inválida", "errors.engine.invalid_youtube_url");

        if (uri.Scheme != "https" && uri.Scheme != "http")
            throw new EngineExecutionException("URL deve ser HTTP(S)", "errors.engine.invalid_youtube_url");

        if (!AllowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            throw new EngineExecutionException("URL não é do YouTube", "errors.engine.invalid_youtube_url");
    }

    private static List<string> BuildArgs(string url, string format, string quality)
    {
        var args = new List<string>
        {
            "--newline",
            "--no-playlist",
            "--restrict-filenames",
            "--extractor-args", "youtube:player_client=ios,android,web",
            "--user-agent", UserAgent,
            "--print", "after_move:filepath",
            "-o", "%(title).100B.%(ext)s",
        };

        if (format == "audio")
        {
            args.AddRange(["-f", "bestaudio/best", "-x", "--audio-format", "mp3", "--audio-quality", "192"]);
        }
        else
        {
            var height = quality switch
            {
                "1080p" => "1080",
                "720p" => "720",
                "480p" => "480",
                _ => null
            };

            if (height is null)
                args.AddRange(["-f", "bestvideo+bestaudio/best", "--merge-output-format", "mp4"]);
            else
                args.AddRange(["-f", $"bestvideo[height<={height}]+bestaudio/best[height<={height}]", "--merge-output-format", "mp4"]);
        }

        args.Add(url);
        return args;
    }
}

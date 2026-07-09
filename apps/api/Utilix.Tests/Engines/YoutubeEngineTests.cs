using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Utilix.Abstractions.Engines;
using Utilix.Api.Engines;
using Utilix.Tests.Infrastructure;

namespace Utilix.Tests.Engines;

public class YoutubeEngineTests
{
    private readonly YoutubeEngine _engine;
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.AddConsole());

    public YoutubeEngineTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Youtube:YtDlpPath", "yt-dlp" } })
            .Build();

        _engine = new YoutubeEngine(new FakeProcessRunner(), config, _loggerFactory.CreateLogger<YoutubeEngine>());
    }

    [Fact]
    public void Metadata_contem_slug_youtube()
    {
        Assert.Equal("youtube", _engine.Metadata.Slug);
        Assert.Equal("video", _engine.Metadata.Options.First(o => o.Key == "format").DefaultValue);
        Assert.Equal("best", _engine.Metadata.Options.First(o => o.Key == "quality").DefaultValue);
    }

    [Fact]
    public async Task ValidateUrl_rejeita_url_invalida()
    {
        var input = new ConversionInput(
            Guid.NewGuid(),
            "youtube",
            null,
            null,
            "not-a-url",
            new Dictionary<string, string>(),
            Path.GetTempPath());

        var progress = new Progress<ProgressReport>();
        var ex = await Assert.ThrowsAsync<EngineExecutionException>(() =>
            _engine.ExecuteAsync(input, progress, CancellationToken.None));

        Assert.Equal("errors.engine.invalid_youtube_url", ex.UserMessageKey);
    }

    [Fact]
    public async Task ValidateUrl_rejeita_url_nao_youtube()
    {
        var input = new ConversionInput(
            Guid.NewGuid(),
            "youtube",
            null,
            null,
            "https://example.com/video",
            new Dictionary<string, string>(),
            Path.GetTempPath());

        var progress = new Progress<ProgressReport>();
        var ex = await Assert.ThrowsAsync<EngineExecutionException>(() =>
            _engine.ExecuteAsync(input, progress, CancellationToken.None));

        Assert.Equal("errors.engine.invalid_youtube_url", ex.UserMessageKey);
    }

    [Theory]
    [InlineData("https://youtube.com/watch?v=abc")]
    [InlineData("https://www.youtube.com/watch?v=abc")]
    [InlineData("https://youtu.be/abc")]
    [InlineData("https://music.youtube.com/watch?v=abc")]
    public async Task ExecuteAsync_aceita_urls_youtube_validas(string url)
    {
        var workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var input = new ConversionInput(
            Guid.NewGuid(),
            "youtube",
            null,
            null,
            url,
            new Dictionary<string, string> { { "format", "video" }, { "quality", "720p" } },
            workingDir);

        var progress = new Progress<ProgressReport>();
        var result = await _engine.ExecuteAsync(input, progress, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("video/mp4", result.MimeType);
        Assert.True(File.Exists(result.OutputPath));

        Directory.Delete(workingDir, recursive: true);
    }
}

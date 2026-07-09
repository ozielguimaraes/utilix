# Engines

Um **engine** é a unidade de conversão. Toda funcionalidade do Utilix é um engine.

## Contrato

```csharp
// Utilix.Abstractions/Engines/IConversionEngine.cs
namespace Utilix.Abstractions.Engines;

public interface IConversionEngine
{
    EngineMetadata Metadata { get; }

    Task<ConversionResult> ExecuteAsync(
        ConversionInput input,
        IProgress<ProgressReport> progress,
        CancellationToken cancellationToken);
}
```

```csharp
public record EngineMetadata(
    string Slug,                      // "youtube", "video", "image-convert"
    string DisplayNameKey,            // chave i18n: "engines.youtube.name"
    string Category,                  // "media" | "image" | "pdf" | "document"
    string[] AcceptedInputs,          // MIME types ou "url"
    string[] OutputFormats,           // ["mp4", "webm", "gif"]
    EngineOption[] Options,           // escolhas do usuário (qualidade, etc.)
    long MaxInputSizeBytes);
```

```csharp
public record ConversionInput(
    Guid JobId,
    string EngineSlug,
    Stream? SourceStream,             // null se input for URL
    string? SourceFileName,
    string? Url,                      // para YouTube e similares
    IReadOnlyDictionary<string, string> Options,
    string WorkingDirectory);         // /tmp/utilix/{jobId}
```

```csharp
public record ConversionResult(
    string OutputPath,                // caminho local; infra sobe pro R2
    string OutputFileName,
    string MimeType,
    long SizeBytes);
```

```csharp
public record ProgressReport(
    int Percent,                      // 0-100; -1 se indeterminado
    string Stage,                     // "uploading", "converting", "finalizing"
    string? Message);
```

## Registry

Engines são registrados no DI e resolvidos por slug.

```csharp
// Program.cs
builder.Services.AddSingleton<IConversionEngine, YoutubeEngine>();
builder.Services.AddSingleton<IConversionEngine, VideoEngine>();
builder.Services.AddSingleton<IConversionEngine, AudioEngine>();
builder.Services.AddSingleton<IConversionEngine, ImageEngine>();
builder.Services.AddSingleton<IConversionEngine, PdfEngine>();
builder.Services.AddSingleton<IConversionEngine, BookletEngine>();
builder.Services.AddSingleton<IConversionEngine, DocumentEngine>();
builder.Services.AddSingleton<EngineRegistry>();
```

```csharp
public class EngineRegistry
{
    private readonly Dictionary<string, IConversionEngine> _map;

    public EngineRegistry(IEnumerable<IConversionEngine> engines)
        => _map = engines.ToDictionary(e => e.Metadata.Slug);

    public IConversionEngine Resolve(string slug)
        => _map.TryGetValue(slug, out var engine)
            ? engine
            : throw new EngineNotFoundException(slug);

    public IEnumerable<EngineMetadata> List()
        => _map.Values.Select(e => e.Metadata);
}
```

`GET /api/engines` retorna `registry.List()` — o frontend monta o catálogo dinamicamente.

## Ciclo de vida de um job

1. Endpoint `Features/Jobs/CreateJob.cs` recebe upload, persiste em R2 como `uploads/{jobId}/source.ext`.
2. Cria `ConversionJob` com `Status = Pending`, enfileira via `IJobQueue.EnqueueAsync`.
3. `JobOrchestrator` (BackgroundService) faz `await foreach` do canal.
4. Para cada job:
   a. Baixa source do R2 para `/tmp/utilix/{jobId}/`.
   b. Resolve engine via `EngineRegistry`.
   c. Cria `IProgress<ProgressReport>` que emite para `IHubContext<ConversionHub>`.
   d. Chama `engine.ExecuteAsync(...)` com `CancellationToken` configurado para timeout.
   e. Faz upload do resultado para `outputs/{jobId}/result.ext`.
   f. Atualiza status para `Completed` e emite `job.completed` via SignalR.
   g. Apaga working directory.
5. Em erro: captura exceção, status `Failed`, emite `job.failed` com mensagem amigável.

## Engines existentes

| Slug | Ferramenta | Entradas | Saídas |
|---|---|---|---|
| `youtube` | `yt-dlp` | URL | mp4, mp3 (com qualidade) |
| `video` | `ffmpeg` | mp4, mov, avi, webm, mkv | mp4, webm, mov, gif |
| `audio` | `ffmpeg` | mp3, wav, ogg, flac, m4a | mp3, wav, ogg, flac, aac |
| `image` | `ImageSharp` | jpg, png, webp, gif, heic | jpg, png, webp, avif (+ resize, compress) |
| `pdf` | `QuestPDF`, `PdfSharpCore`, `gs` | pdf | pdf (merge/split/compress) |
| `booklet` | `PdfSharpCore` (porta do `booklet_split.py`) | pdf | pdf (livreto 2-em-1) |
| `document` | `Gotenberg` HTTP | docx, pptx, xlsx, odt | pdf |

## Como adicionar um engine novo

Exemplo: adicionar conversão de GIF para sticker WebP.

### Passo 1 — Criar projeto ou adicionar ao existente

Se é mídia, vai em `Utilix.Engines.Media`. Se é novo tipo, cria `Utilix.Engines.Sticker/`.

### Passo 2 — Implementar `IConversionEngine`

```csharp
namespace Utilix.Engines.Media;

public class StickerEngine : IConversionEngine
{
    private readonly IProcessRunner _process;

    public StickerEngine(IProcessRunner process) => _process = process;

    public EngineMetadata Metadata => new(
        Slug: "sticker",
        DisplayNameKey: "engines.sticker.name",
        Category: "image",
        AcceptedInputs: ["image/gif", "video/mp4"],
        OutputFormats: ["webp"],
        Options: [
            new EngineOption("size", "select", defaultValue: "512",
                choices: ["256", "512"])
        ],
        MaxInputSizeBytes: 50 * 1024 * 1024);

    public async Task<ConversionResult> ExecuteAsync(
        ConversionInput input,
        IProgress<ProgressReport> progress,
        CancellationToken ct)
    {
        progress.Report(new(0, "preparing", null));

        var size = input.Options.GetValueOrDefault("size", "512");
        var sourcePath = Path.Combine(input.WorkingDirectory, input.SourceFileName!);
        var outputPath = Path.Combine(input.WorkingDirectory, "sticker.webp");

        var args = $"-i \"{sourcePath}\" -vcodec libwebp -loop 0 " +
                   $"-preset default -an -vsync 0 " +
                   $"-s {size}:{size} \"{outputPath}\"";

        await _process.RunAsync("ffmpeg", args, progress, ct);

        progress.Report(new(100, "done", null));

        return new ConversionResult(
            OutputPath: outputPath,
            OutputFileName: "sticker.webp",
            MimeType: "image/webp",
            SizeBytes: new FileInfo(outputPath).Length);
    }
}
```

### Passo 3 — Registrar no DI

```csharp
// Utilix.Api/Program.cs
builder.Services.AddSingleton<IConversionEngine, StickerEngine>();
```

Em `Program.cs` o registro é declarativo, com as features montando suas próprias rotas:

```csharp
var jobs = app.MapGroup("/api/jobs");
CreateJob.Map(jobs);
GetJob.Map(jobs);
CancelJob.Map(jobs);

var engines = app.MapGroup("/api/engines");
ListEngines.Map(engines);
```

### Passo 4 — Traduções

```json
// apps/web/src/assets/i18n/pt-BR.json
"engines": {
  "sticker": {
    "name": "Figurinha WhatsApp",
    "description": "Converte GIF ou vídeo em figurinha animada",
    "options": {
      "size": { "label": "Tamanho", "choices": { "256": "256px", "512": "512px" } }
    }
  }
}
```

### Passo 5 — Teste

```csharp
// Utilix.Tests/Engines/StickerEngineTests.cs
public class StickerEngineTests
{
    [Fact]
    public async Task Converte_gif_para_webp_512()
    {
        // arrange com FakeProcessRunner
        // act
        // assert filename + mime type
    }
}
```

**Pronto.** Nenhuma mudança em controller, queue ou frontend genérico. O novo engine aparece automaticamente em `GET /api/engines` e no catálogo da home.

## Padrões importantes

### Timeouts

Todo engine respeita `CancellationToken`. `JobOrchestrator` aplica timeout global (default 10 min, configurável por engine em `Metadata`).

### Processos externos

Use sempre `IProcessRunner` (contrato em `Utilix.Abstractions/Process/`, implementação em `Utilix.Api/Infrastructure/Process/`). Ele:

- Configura `ProcessStartInfo` com `RedirectStandardOutput/Error`.
- Parseia stderr do FFmpeg/yt-dlp para extrair progresso (regex por ferramenta).
- Mata o processo se `CancellationToken` disparar.
- Loga comando e exit code.

Nunca chame `Process.Start` diretamente.

### Working directory

Cada job tem seu `/tmp/utilix/{jobId}/`. Engine só lê e escreve dentro dele. `JobOrchestrator` limpa após upload do resultado.

### Progresso

- FFmpeg: parsear `out_time_ms` de `-progress pipe:1`.
- yt-dlp: parsear linhas `[download] XX.X%` do stderr.
- ImageSharp: não expõe progresso; reportar `stage` apenas.
- Gotenberg: sem progresso; emitir `(−1, "converting", null)` e depois `100` ao terminar.

### Segurança

- **Nunca** interpolar input do usuário direto em argumentos de processo. Sempre usar arrays de args ou aspas explícitas em caminhos que você mesmo gerou.
- Validar MIME types contra `Metadata.AcceptedInputs` antes de enfileirar.
- Rejeitar arquivos acima de `MaxInputSizeBytes` no controller, não no engine.

## Erros

Engine joga exceção tipada:

```csharp
throw new EngineExecutionException(
    "FFmpeg falhou: codec não suportado",
    userMessageKey: "errors.engine.unsupported_codec");
```

`JobOrchestrator` captura, marca job como `Failed`, e emite via SignalR com `userMessageKey` traduzível.

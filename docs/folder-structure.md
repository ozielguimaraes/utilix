# Estrutura de pastas

Monorepo com workspace npm/pnpm na raiz e solution .NET 10.

**Filosofia da estrutura:** feature folders + REPR (Request-Endpoint-Response) com Minimal APIs no backend. Signals + standalone components no frontend. Sem camadas cerimoniais. Ver [ADR 0005](adr/0005-minimal-apis-over-clean-arch.md).

```
utilix/
│
├── apps/
│   ├── api/                                       # Backend ASP.NET Core 10
│   │   ├── Utilix.Abstractions/                   # Só contratos, zero deps externas
│   │   │   ├── Engines/
│   │   │   │   ├── IConversionEngine.cs
│   │   │   │   ├── EngineMetadata.cs
│   │   │   │   ├── ConversionInput.cs
│   │   │   │   ├── ConversionResult.cs
│   │   │   │   └── ProgressReport.cs
│   │   │   ├── Jobs/
│   │   │   │   └── IJobQueue.cs
│   │   │   ├── Storage/
│   │   │   │   └── IFileStorage.cs
│   │   │   └── Process/
│   │   │       └── IProcessRunner.cs
│   │   │
│   │   ├── Utilix.Api/                            # Host + features + infra
│   │   │   ├── Features/                          # REPR: 1 arquivo = 1 endpoint
│   │   │   │   ├── Jobs/
│   │   │   │   │   ├── CreateJob.cs               # POST /api/jobs
│   │   │   │   │   ├── GetJob.cs                  # GET  /api/jobs/{id}
│   │   │   │   │   ├── CancelJob.cs               # DELETE /api/jobs/{id}
│   │   │   │   │   ├── Job.cs                     # record do domínio
│   │   │   │   │   └── JobStore.cs                # dict concorrente em memória
│   │   │   │   ├── Engines/
│   │   │   │   │   └── ListEngines.cs             # GET /api/engines
│   │   │   │   └── Files/
│   │   │   │       └── DownloadFile.cs            # GET /api/files/{id}
│   │   │   │
│   │   │   ├── Hubs/
│   │   │   │   └── ConversionHub.cs               # SignalR: progress, completed, failed
│   │   │   │
│   │   │   ├── Infrastructure/                    # Pasta, não projeto
│   │   │   │   ├── Storage/
│   │   │   │   │   ├── R2FileStorage.cs           # Produção (S3-compatible)
│   │   │   │   │   └── LocalFileStorage.cs        # Desenvolvimento
│   │   │   │   ├── Queue/
│   │   │   │   │   ├── ChannelJobQueue.cs         # MVP: in-memory
│   │   │   │   │   └── JobOrchestrator.cs         # BackgroundService consumer
│   │   │   │   ├── Process/
│   │   │   │   │   └── ProcessRunner.cs           # Wrapper de Process.Start
│   │   │   │   ├── Engines/
│   │   │   │   │   └── EngineRegistry.cs          # resolve por slug
│   │   │   │   └── Cleanup/
│   │   │   │       └── CleanupService.cs          # deleta blobs > 1h
│   │   │   │
│   │   │   ├── appsettings.json
│   │   │   ├── appsettings.Development.json
│   │   │   └── Program.cs                         # DI + MapGroup + SignalR
│   │   │
│   │   ├── Utilix.Engines.Media/                  # FFmpeg
│   │   │   ├── VideoEngine.cs                     # mp4, webm, mov, gif
│   │   │   ├── AudioEngine.cs                     # mp3, wav, ogg, flac, m4a
│   │   │   └── FFmpegArguments.cs                 # builders
│   │   │
│   │   ├── Utilix.Engines.Image/                  # ImageSharp (puro .NET)
│   │   │   └── ImageEngine.cs                     # jpg, png, webp, avif, resize
│   │   │
│   │   ├── Utilix.Engines.Pdf/                    # QuestPDF + PdfSharpCore + gs
│   │   │   ├── PdfEngine.cs                       # merge, split, compress
│   │   │   └── BookletEngine.cs                   # porta do booklet_split.py
│   │   │
│   │   ├── Utilix.Engines.Document/               # Gotenberg HTTP client
│   │   │   └── DocumentEngine.cs                  # docx/pptx/xlsx → pdf
│   │   │
│   │   ├── Utilix.Engines.Youtube/                # yt-dlp process
│   │   │   └── YoutubeEngine.cs                   # porta do app.py
│   │   │
│   │   └── Utilix.Tests/
│   │       ├── Features/                          # testes de endpoint (WebApplicationFactory)
│   │       ├── Engines/                           # testes por engine
│   │       └── Infrastructure/
│   │
│   └── web/                                       # Angular 21 PWA
│       ├── src/
│       │   ├── app/
│       │   │   ├── features/                      # Uma pasta = uma rota
│       │   │   │   ├── home/
│       │   │   │   │   ├── home.ts                # standalone component
│       │   │   │   │   ├── home.html
│       │   │   │   │   ├── home.scss
│       │   │   │   │   └── home.routes.ts
│       │   │   │   ├── convert/
│       │   │   │   │   ├── convert.ts
│       │   │   │   │   ├── convert.html
│       │   │   │   │   ├── convert.scss
│       │   │   │   │   ├── conversion-store.ts   # signals exportados
│       │   │   │   │   └── convert.routes.ts
│       │   │   │   ├── youtube/
│       │   │   │   │   ├── youtube.ts
│       │   │   │   │   └── youtube.routes.ts
│       │   │   │   └── booklet/
│       │   │   │
│       │   │   ├── shared/
│       │   │   │   └── ui/                        # Design system
│       │   │   │       ├── button/
│       │   │   │       ├── input/
│       │   │   │       ├── dropzone/
│       │   │   │       ├── progress/
│       │   │   │       ├── card/
│       │   │   │       ├── badge/
│       │   │   │       ├── modal/
│       │   │   │       ├── toast/
│       │   │   │       └── icon/
│       │   │   │
│       │   │   ├── core/                          # Providers app-wide (funções)
│       │   │   │   ├── api.ts                     # fetch wrappers
│       │   │   │   ├── signalr.ts                 # conexão singleton
│       │   │   │   ├── i18n.ts                    # Transloco setup
│       │   │   │   ├── theme.ts                   # dark mode toggle
│       │   │   │   └── error-interceptor.ts       # functional interceptor
│       │   │   │
│       │   │   ├── app.ts                         # root standalone
│       │   │   ├── app.config.ts                  # providers + bootstrap
│       │   │   └── app.routes.ts
│       │   │
│       │   ├── assets/
│       │   │   ├── i18n/
│       │   │   │   ├── pt-BR.json
│       │   │   │   └── en.json
│       │   │   └── icons/
│       │   │
│       │   ├── styles/
│       │   │   ├── tokens.scss                    # design tokens (cores, spacing)
│       │   │   ├── reset.scss
│       │   │   └── global.scss
│       │   │
│       │   ├── index.html
│       │   ├── main.ts
│       │   └── manifest.webmanifest               # PWA
│       │
│       ├── ngsw-config.json                       # Service Worker (PWA)
│       ├── angular.json
│       ├── tsconfig.json
│       └── package.json
│
├── services/
│   └── gotenberg/                                 # Serviço separado para docs
│       ├── Dockerfile                             # imagem gotenberg/gotenberg:8
│       ├── cloudrun.yaml
│       └── README.md
│
├── packages/
│   └── shared-types/                              # TS gerado do OpenAPI
│       ├── src/
│       └── package.json
│
├── legacy/                                        # Código original preservado
│   ├── DownloadFromYoutube/                       # Fonte da porta C# do YoutubeEngine
│   ├── booklet.py
│   ├── booklet_split.py                           # Algoritmo usado no BookletEngine
│   └── booklet_ready_print.py
│
├── docker/
│   ├── Dockerfile.api                             # .NET 10 + ffmpeg + yt-dlp + gs
│   ├── docker-compose.yml                         # api + gotenberg + minio (dev)
│   └── .dockerignore
│
├── .github/
│   └── workflows/
│       ├── api-deploy.yml                         # Build + push + deploy Cloud Run
│       ├── web-deploy.yml                         # Build + deploy Cloudflare Pages
│       └── ci.yml                                 # Tests em PR
│
├── docs/
│   ├── README.md
│   ├── architecture.md
│   ├── folder-structure.md                        # Este arquivo
│   ├── design-system.md
│   ├── engines.md
│   ├── api.md
│   ├── deployment.md
│   ├── roadmap.md
│   └── adr/
│       ├── 0001-stack-choice.md
│       ├── 0002-engine-abstraction.md
│       ├── 0003-gotenberg-for-documents.md
│       ├── 0004-storage-and-retention.md
│       └── 0005-minimal-apis-over-clean-arch.md
│
├── Utilix.sln                                     # Solution .NET 10
├── package.json                                   # Workspace raiz
├── .gitignore
├── .editorconfig
└── README.md
```

## Projetos .NET — o mínimo necessário

| Projeto | Razão de existir |
|---|---|
| `Utilix.Abstractions` | Contratos (`IConversionEngine`, `IFileStorage`, etc.). Zero deps. Engines referenciam daqui sem puxar o host |
| `Utilix.Api` | Host web, features (endpoints), hubs, infra in-process |
| `Utilix.Engines.Media` | Isola FFmpeg wrapper |
| `Utilix.Engines.Image` | Isola ImageSharp |
| `Utilix.Engines.Pdf` | Isola QuestPDF, PdfSharpCore, Ghostscript |
| `Utilix.Engines.Document` | Isola HttpClient para Gotenberg |
| `Utilix.Engines.Youtube` | Isola lógica de yt-dlp |
| `Utilix.Tests` | Testes de tudo |

**Não tem:**
- ❌ `Utilix.Core` / `Utilix.Domain` — domínio é raso, vive em `Features/Jobs/Job.cs` como record
- ❌ `Utilix.Application` — não há camada de use cases; endpoint **é** o use case
- ❌ `Utilix.Infrastructure` — vira pasta dentro de `Utilix.Api/`

Quando aparecer regra de negócio real (quotas, billing, auth com regras), aí sim extrai. Não antes.

## Convenções

### Backend (.NET 10)

- **Features** agrupam endpoints por recurso. Um arquivo por endpoint usando REPR:
  ```csharp
  public record CreateJobRequest(...);
  public record CreateJobResponse(...);
  public static class CreateJob
  {
      public static void Map(RouteGroupBuilder g) => g.MapPost("/", Handle);
      static async Task<...> Handle(...) { ... }
  }
  ```
- **Minimal APIs** em vez de Controllers. `Program.cs` faz `MapGroup` e chama `Feature.Map(group)`.
- **`Infrastructure/`** só contém coisas que vários features usam (storage, queue, process runner). Se for específico de uma feature, fica dentro dela.
- **Engines** são projetos separados porque têm dependências binárias ou de pacote diferentes — isolamento tem razão técnica real.
- **Testes** em `Utilix.Tests` espelhando a estrutura: `Features/Jobs/CreateJobTests.cs`.

### Frontend (Angular 21)

- **Standalone components** em todo lugar. Sem NgModule.
- **Signals** para estado. `signal()`, `computed()`, `effect()`. Sem NgRx.
- **Store por feature** = um arquivo `*-store.ts` com signals exportados e funções de mutação. Sem `@Injectable`, sem provider.
- **`core/`** tem funções e providers, não classes gigantes. DI funcional com `inject()`.
- **`shared/ui/`** não fala com API nem com core — componentes puros de apresentação.
- **Features** importam de `core/` e `shared/`. Uma feature não importa de outra.
- **Zoneless** change detection (default do Angular 21) — importar `provideZonelessChangeDetection()` no config.

### Legado

- `legacy/` **não vai em produção**. Referência para portar `booklet_split.py` e as opções de `yt-dlp` do `app.py`.
- Não deletar até engines portados e testados.

# Arquitetura

## Visão geral

Utilix é uma plataforma web de utilitários de conversão. O desenho central é:

- **Angular 21 PWA** (standalone + signals + zoneless) consumido no celular e desktop, com design system baseado em tokens CSS.
- **API ASP.NET Core 10** com Minimal APIs em padrão REPR (Request-Endpoint-Response): um arquivo por endpoint.
- **Engines plugáveis**: cada utilitário (vídeo, imagem, PDF, booklet, YouTube, documento) implementa `IConversionEngine` e é registrado via DI.
- **Gotenberg** como microsserviço serverless separado, só acionado para conversão de documentos pesados (docx/pptx/xlsx → pdf).
- **Cloudflare R2** para storage temporário, com egress zero (usuário baixa o arquivo sem custo de banda).
- **SignalR** para progresso em tempo real, com fallback de polling.

**Sem Clean Architecture.** O domínio do Utilix é raso (um `Job` com status e resultado). Camadas (Domain/Application/Infrastructure) seriam cerimônia sem ganho. Ver [ADR 0005](adr/0005-minimal-apis-over-clean-arch.md).

## Diagrama

```
┌─────────────────────────────────────────────────┐
│  Angular PWA (Cloudflare Pages)                 │
│  - Mobile-first, offline shell                  │
│  - i18n PT-BR/EN automático                     │
│  - Upload chunked, drag&drop                    │
│  - SignalR client (WebSocket + polling fallback)│
└──────────────────────┬──────────────────────────┘
                       │ HTTPS
┌──────────────────────▼──────────────────────────┐
│  Utilix.Api (Google Cloud Run, scale-to-zero)   │
│                                                 │
│  Features (Minimal APIs + REPR)  Hubs           │
│  ├─ Jobs/ CreateJob, GetJob,...  └─ Conversion  │
│  ├─ Engines/ ListEngines            Hub         │
│  └─ Files/ DownloadFile                         │
│                                                 │
│  Middleware: RateLimit, CORS, Logging           │
│                                                 │
│  ┌──────────────────────────────────────────┐   │
│  │ JobQueue: Channel<ConversionJob>         │   │
│  │  → BackgroundService consome             │   │
│  │  (MVP in-memory; v1.2 → Hangfire+Redis)  │   │
│  └────────────────┬─────────────────────────┘   │
│                   │                             │
│  ┌────────────────▼─────────────────────────┐   │
│  │ EngineRegistry (resolve por slug)        │   │
│  ├──────────────────────────────────────────┤   │
│  │ IConversionEngine                        │   │
│  │  ├─ YoutubeEngine    (yt-dlp)            │   │
│  │  ├─ VideoEngine      (FFmpeg)            │   │
│  │  ├─ AudioEngine      (FFmpeg)            │   │
│  │  ├─ ImageEngine      (ImageSharp)        │   │
│  │  ├─ PdfEngine        (QuestPDF/PdfSharp) │   │
│  │  ├─ BookletEngine    (PdfSharpCore)      │   │
│  │  └─ DocumentEngine   ─┐                  │   │
│  └───────────────────────┼──────────────────┘   │
│                          │                      │
│  IFileStorage ───► Cloudflare R2                │
└──────────────────────────┼──────────────────────┘
                           │ HTTP
┌──────────────────────────▼──────────────────────┐
│  Gotenberg (Cloud Run, scale-to-zero)           │
│  - LibreOffice headless + Chromium              │
│  - API HTTP: /forms/libreoffice/convert         │
│  - Acionado só quando DocumentEngine pedir      │
└─────────────────────────────────────────────────┘
```

## Fluxos principais

### Fluxo 1 — Conversão por upload

1. Usuário arrasta arquivo na dropzone.
2. Frontend faz `POST /api/jobs` (multipart) com o arquivo + `engineSlug` + `options`.
3. API persiste o blob em R2 (`uploads/{jobId}/source.ext`), cria `ConversionJob`, enfileira.
4. API responde `202 Accepted` com `{ jobId }`.
5. Frontend abre conexão SignalR e entra no grupo `job:{jobId}`.
6. `BackgroundService` consome do canal → resolve engine → executa.
7. Engine processa e grava saída em R2 (`outputs/{jobId}/result.ext`).
8. API emite `job.completed` via SignalR com `downloadUrl` assinado.
9. Frontend dispara download do link assinado (válido por 1h).
10. Job de cleanup remove os blobs após 1h (ver `adr/0004`).

### Fluxo 2 — Conversão sem upload (YouTube)

1. Usuário cola URL + escolhe formato/qualidade.
2. `POST /api/jobs` com `engineSlug: "youtube"` + `input: { url }`.
3. API enfileira; `YoutubeEngine` invoca `yt-dlp` como processo.
4. Saída salva em R2, resto do fluxo igual ao anterior.

### Fluxo 3 — Progresso em tempo real

- Engines emitem eventos `IProgress<ProgressReport>` (percent, stage, message).
- `JobOrchestrator` encaminha para `IHubContext<ConversionHub>.Clients.Group("job:{id}")`.
- Cliente escuta `progress` e atualiza a barra.
- Se WebSocket falhar, cliente cai para polling `GET /api/jobs/{id}` a cada 2s (compatível com o padrão atual do `app.py`).

## Decisões de stack

| Decisão | Motivo | ADR |
|---|---|---|
| ASP.NET Core 10 + Angular 21 | LTS mais recente; DX forte; SignalR integrado; zoneless no Angular | [0001](adr/0001-stack-choice.md) |
| Engines como processos externos | Desacopla de libs; fácil trocar binário; sandbox natural | [0002](adr/0002-engine-abstraction.md) |
| Gotenberg serverless | LibreOffice pesa 1.5GB no container; serverless separado escala a zero | [0003](adr/0003-gotenberg-for-documents.md) |
| R2 + retenção 1h | Egress zero; privacidade por padrão | [0004](adr/0004-storage-and-retention.md) |
| Minimal APIs + REPR (sem Clean Arch) | Domínio raso não justifica camadas; 1 arquivo = 1 endpoint | [0005](adr/0005-minimal-apis-over-clean-arch.md) |

## Pontos de extensibilidade

- **Novo utilitário**: implementar `IConversionEngine`, registrar no DI, adicionar metadata. Nenhuma mudança em controller, queue ou frontend genérico. Ver [engines.md](engines.md).
- **Novo provider de storage**: implementar `IFileStorage`. Troca-se R2 por S3 ou local sem tocar em engine.
- **Nova fila**: implementar `IJobQueue`. MVP usa `Channel<T>`, v1.2 migra para Hangfire sem tocar em engine.
- **Novo progresso**: `ConversionHub` já é agnóstico. Qualquer canal (SSE, webhook) pode ser adicionado.

## Segurança

- **Rate limit** por IP usando `Microsoft.AspNetCore.RateLimiting` (fixed window, 10 jobs/min por IP no MVP).
- **Limite de upload**: 500 MB no MVP; configurável por engine.
- **Sanitização**: nomes de arquivo passam por `Path.GetRandomFileName()` internamente. Usuário nunca controla caminho.
- **Sandbox de processos**: cada execução tem timeout (`CancellationToken`), working directory isolado em `/tmp/utilix/{jobId}`, resource limits via `ProcessStartInfo`.
- **URLs assinadas**: downloads usam URLs pré-assinadas do R2 com TTL de 1h.
- **CORS**: só libera origem do frontend em produção.
- **Sem execução de código do usuário**: todas as ferramentas são CLIs controladas.

## Observabilidade

- Logs estruturados via `Serilog` → stdout (Cloud Run agrega automaticamente).
- Correlation ID por request propagado para o engine via `ILogger.BeginScope`.
- Métricas expostas em `/metrics` (OpenTelemetry) — opcional no MVP.

## Portabilidade

Toda a stack roda em Docker. Se Cloud Run ficar caro, o mesmo `Dockerfile.api` sobe em:

- Hetzner Cloud + Docker Compose
- Fly.io
- AWS Fargate
- Azure Container Apps

O único serviço específico de nuvem é o R2 (storage), abstraído atrás de `IFileStorage` — substituível por `LocalFileStorage` (desenvolvimento) ou `S3FileStorage`.

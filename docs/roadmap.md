# Roadmap

## Filosofia

- **MVP enxuto**: entregar valor real com o mínimo possível. Sem auth, sem contas, sem persistência.
- **Reaproveitar**: código Python existente é referência; porta para C# acontece conforme necessidade.
- **Evolução guiada por uso**: fila simples até realmente precisar de Hangfire; storage local até realmente precisar de R2 em prod.

## MVP (4 semanas)

### Semana 1 — fundação

- [ ] Monorepo configurado: `Utilix.sln`, workspace npm, scripts raiz
- [ ] `apps/api`: `Utilix.Abstractions` + `Utilix.Api` (Minimal APIs) com `/health`
- [ ] Contratos em `Utilix.Abstractions`: `IConversionEngine`, `IJobQueue`, `IFileStorage`, `IProcessRunner`
- [ ] Infra em `Utilix.Api/Infrastructure/`: `ChannelJobQueue`, `JobOrchestrator`, `EngineRegistry`, `LocalFileStorage`
- [ ] Features esqueleto: `Features/Jobs/CreateJob.cs`, `GetJob.cs`, `CancelJob.cs`
- [ ] `apps/web`: Angular 21 standalone + signals + zoneless, rotas básicas, tokens.scss
- [ ] Componentes base: Button, Input, Card, Dropzone, Progress
- [ ] `docker-compose.yml` local (api + minio)
- [ ] Testes: CI em PR

**Saída da semana:** app sobe local, dropzone funciona, engine fake retorna arquivo.

### Semana 2 — primeiros engines

- [ ] `YoutubeEngine`: porta do `app.py` com opções de `player_client` (ios/android/web)
- [ ] `VideoEngine`: mp4↔webm, extrair áudio, gerar gif
- [ ] `ProcessRunner` com parse de progresso FFmpeg e yt-dlp
- [ ] Endpoints: `POST /api/jobs`, `GET /api/jobs/{id}`, `DELETE /api/jobs/{id}`
- [ ] `ConversionHub` SignalR com eventos `progress`, `completed`, `failed`
- [ ] Frontend consome `/api/engines` e monta catálogo

**Saída da semana:** converte vídeo real e baixa YouTube pelo navegador.

### Semana 3 — frontend completo

- [ ] Home com catálogo + busca
- [ ] Página `/convert/:slug` genérica
- [ ] Página `/convert/youtube` específica (cola URL)
- [ ] i18n PT-BR + EN com detecção automática
- [ ] Toggle de idioma manual (persiste em localStorage)
- [ ] Dark mode com detecção + toggle
- [ ] PWA: manifest, service worker, instalável
- [ ] Mobile testado em iOS e Android reais
- [ ] Bottom sheet no mobile para opções

**Saída da semana:** produto usável no celular, instalável como PWA.

### Semana 4 — produção

- [ ] `ImageEngine` (ImageSharp): jpg↔png↔webp, resize, compress
- [ ] `R2FileStorage` + URLs pré-assinadas
- [ ] `CleanupService` (apaga blobs > 1h)
- [ ] Rate limiting (10 jobs/min/IP)
- [ ] `Dockerfile.api` com ffmpeg + yt-dlp + ghostscript
- [ ] Deploy: Cloud Run API + Cloudflare Pages + R2
- [ ] CI deploy automático na main
- [ ] Domínio custom + TLS
- [ ] Teste de carga leve (100 jobs concorrentes)

**Saída do MVP:** produto em produção com 4 engines (YouTube, Video, Audio, Image).

## Pós-MVP

### v1.1 — PDF (2 semanas)

- `PdfEngine`: merge, split, compress (Ghostscript)
- `BookletEngine`: porta direta do `booklet_split.py`
- UI dedicada pra PDF com preview de páginas
- Suporte a múltiplos arquivos de entrada (merge)

### v1.2 — Documentos (1-2 semanas)

- `services/gotenberg/` deploy em Cloud Run separado
- `DocumentEngine` com HttpClient autenticado
- Suporte: docx, pptx, xlsx, odt, ods, odp → pdf
- Teste de timeout longo (documentos grandes)

### v1.3 — Batch e histórico (1 semana)

- Upload múltiplo com fila visual no frontend
- Download em zip de múltiplos resultados
- Histórico por sessão em localStorage (últimas 20 conversões)
- Cancelar todos / baixar todos

### v1.4 — Robustez (1 semana)

- Migração para Hangfire + Redis (fila persistente)
- Retry automático em falhas transitórias
- Dead letter queue para jobs que falham 3x
- Dashboard interno `/admin/jobs` (protegido)

### v2.0 — Contas (3-4 semanas)

- Auth via magic link (email) ou OAuth (Google)
- Histórico persistente
- Plano free (3 conversões simultâneas) / pro (ilimitado + arquivos maiores)
- Integração com Stripe
- API keys para uso programático

### v2.1 — Escala (2 semanas)

- Workers horizontais (separar API de Worker)
- Redis compartilhado para fila
- Métricas OpenTelemetry → Grafana Cloud (tier grátis)
- CDN na frente do R2 para downloads

### v2.2 — Utilitários avançados (conforme demanda)

- OCR (Tesseract) para PDF e imagens
- Transcrição de áudio (Whisper via Cloud Run GPU sob demanda, ou API paga)
- Tradução de documentos
- Remoção de fundo de imagens (rembg ou API paga)
- Compressão agressiva de vídeo (H.265, AV1)

### v3.0 — Desktop (exploratório)

- Empacotar Angular em Tauri (~10MB, Rust-based)
- Engines rodam 100% local (sem upload)
- Privacidade total, funciona offline
- Distribuir em macOS, Windows, Linux

## Métricas de sucesso

Cada fase tem metas mensuráveis:

| Fase | Meta |
|---|---|
| MVP | 100 conversões bem-sucedidas sem intervenção manual |
| v1.2 | 1k conversões/mês, uptime > 99% |
| v1.4 | 10k conversões/mês, p95 < 30s para imagem/áudio |
| v2.0 | 100 usuários registrados, 10 pagantes |
| v2.1 | 100k conversões/mês sem incidente |

## O que **não** vamos fazer (e por quê)

- **Editor de vídeo / imagem inline**: escopo diferente. Utilix é conversão, não edição.
- **Hospedar arquivos permanentemente**: privacidade e custo. Retenção 1h é feature, não limitação.
- **App nativo mobile**: PWA cobre 95% do valor com 10% do esforço.
- **Conversões exóticas** (DWG, PSD, etc.): entram só se aparecer demanda real.

## Decisões em aberto

- **Monetização**: ads vs freemium vs doação. Decidir na v1.4.
- **Hosting de produção a longo prazo**: Cloud Run, Fly.io ou Hetzner. Reavaliar a cada 3 meses conforme custo.
- **Modelo de IA local**: embarcar algum modelo pequeno (ex: rembg) aumenta imagem do container. Avaliar em v2.2.

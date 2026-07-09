# Utilix

**[English](#english) • [Português](#português)**

---

## English

Utilix is a **zero-cost file conversion platform** inspired by Smallpdf and CloudConvert. Mobile-first PWA with pluggable conversion engines (FFmpeg, ImageSharp, QuestPDF, Gotenberg, yt-dlp) running on serverless infrastructure.

### Key Features

- **Zero cost at idle**: Scale-to-zero architecture. Pay only when converting.
- **Mobile-first**: Responsive PWA with offline support and local file history.
- **Privacy by default**: Files auto-deleted after 1 hour.
- **Supports**: Video, audio, images, PDFs, documents, and YouTube downloads.

### Tech Stack

- **Backend**: ASP.NET Core 10 (Minimal APIs + REPR)
- **Frontend**: Angular 21 PWA (standalone, signals, zoneless)
- **Infrastructure**: Google Cloud Run + Cloudflare Pages + Cloudflare R2
- **Engines**: FFmpeg, yt-dlp, ImageSharp, QuestPDF, PdfSharpCore, Gotenberg

### Quick Start

```bash
git clone https://github.com/ozielsilva/utilix.git && cd utilix

# Start development environment
docker compose up -d

# Frontend development
cd apps/web && npm ci && npm run start
# Opens at http://localhost:4200
```

### Documentation

Full documentation in [`docs/`](docs/README.md):

- [Architecture](docs/architecture.md) — System design and data flows
- [Roadmap](docs/roadmap.md) — 4-week MVP + evolution plan
- [Engines](docs/engines.md) — Adding new conversion engines
- [Deployment](docs/deployment.md) — Production deployment guide
- [Design System](docs/design-system.md) — UI tokens and components

### Roadmap

| Phase | Duration | Scope |
|---|---|---|
| MVP | 4 weeks | YouTube + Video + Audio + Image, production-ready |
| v1.1 | 2 weeks | PDF tools (merge, split, compress, booklet) |
| v1.2 | 1-2 weeks | Document conversion (docx/pptx/xlsx → pdf) |
| v1.3 | 1 week | Batch processing, zip, local history |
| v1.4 | 1 week | Hangfire + Redis, admin dashboard |
| v2.0 | 3-4 weeks | User accounts, freemium plans, Stripe integration |

### License

TBD

---

## Português

Utilix é uma **plataforma de conversão de arquivos com custo zero** inspirada em Smallpdf e CloudConvert. PWA mobile-first com engines de conversão plugáveis (FFmpeg, ImageSharp, QuestPDF, Gotenberg, yt-dlp) rodando em infraestrutura serverless.

### Principais Características

- **Custo zero no ócio**: Arquitetura scale-to-zero. Pague apenas ao converter.
- **Mobile-first**: PWA responsiva com suporte offline e histórico local.
- **Privacidade por padrão**: Arquivos auto-deletados após 1 hora.
- **Suporta**: Vídeos, áudio, imagens, PDFs, documentos e downloads do YouTube.

### Stack Tecnológico

- **Backend**: ASP.NET Core 10 (Minimal APIs + REPR)
- **Frontend**: Angular 21 PWA (standalone, signals, zoneless)
- **Infraestrutura**: Google Cloud Run + Cloudflare Pages + Cloudflare R2
- **Engines**: FFmpeg, yt-dlp, ImageSharp, QuestPDF, PdfSharpCore, Gotenberg

### Quick Start

```bash
git clone https://github.com/ozielsilva/utilix.git && cd utilix

# Subir ambiente local
docker compose up -d

# Desenvolvimento frontend
cd apps/web && npm ci && npm run start
# Abre em http://localhost:4200
```

### Documentação

Documentação completa em [`docs/`](docs/README.md):

- [Arquitetura](docs/architecture.md) — Design do sistema e fluxos de dados
- [Roadmap](docs/roadmap.md) — MVP 4 semanas + plano de evolução
- [Engines](docs/engines.md) — Adicionar novos engines de conversão
- [Deployment](docs/deployment.md) — Guia de deploy em produção
- [Design System](docs/design-system.md) — Tokens e componentes UI

### Roadmap

| Fase | Duração | Escopo |
|---|---|---|
| MVP | 4 semanas | YouTube + Vídeo + Áudio + Imagem, pronto para produção |
| v1.1 | 2 semanas | Ferramentas PDF (merge, split, compress, livreto) |
| v1.2 | 1-2 semanas | Conversão de documentos (docx/pptx/xlsx → pdf) |
| v1.3 | 1 semana | Processamento em lote, zip, histórico local |
| v1.4 | 1 semana | Hangfire + Redis, painel administrativo |
| v2.0 | 3-4 semanas | Contas de usuário, planos freemium, Stripe |

### Licença

A definir

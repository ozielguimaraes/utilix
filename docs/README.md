# Documentação Utilix

Plataforma web de utilitários de conversão. Estilo Smallpdf/CloudConvert, com foco em UX mobile-first e custo operacional próximo de zero.

## Visão rápida

- **Backend**: ASP.NET Core 10 (Minimal APIs + REPR) com abstração de `IConversionEngine` para cada utilitário.
- **Frontend**: Angular 21 PWA (standalone + signals + zoneless), mobile-first, design system via tokens CSS.
- **Deploy**: Google Cloud Run (scale-to-zero) + Cloudflare Pages + Cloudflare R2.
- **Engines**: FFmpeg, yt-dlp, ImageSharp, QuestPDF, PdfSharpCore, Gotenberg.

## Índice

### Arquitetura e código
- [architecture.md](architecture.md) — diagrama geral, fluxos e decisões de stack
- [folder-structure.md](folder-structure.md) — árvore anotada do monorepo
- [engines.md](engines.md) — contrato `IConversionEngine` e como adicionar um novo
- [api.md](api.md) — endpoints REST e eventos SignalR

### Produto e UX
- [design-system.md](design-system.md) — tokens, componentes e padrões de UX
- [roadmap.md](roadmap.md) — MVP (4 semanas) e evolução pós-MVP

### Operação
- [deployment.md](deployment.md) — deploy passo a passo + custos + plano B

### Decisões arquiteturais (ADRs)
- [0001 — Escolha de stack](adr/0001-stack-choice.md)
- [0002 — Abstração de engine como processos](adr/0002-engine-abstraction.md)
- [0003 — Gotenberg serverless para documentos](adr/0003-gotenberg-for-documents.md)
- [0004 — Storage em R2 com retenção 1h](adr/0004-storage-and-retention.md)
- [0005 — Minimal APIs com REPR em vez de Clean Architecture](adr/0005-minimal-apis-over-clean-arch.md)

## Por onde começar

- **Entender a ideia em 5 minutos**: `architecture.md`
- **Adicionar um engine novo**: `engines.md`
- **Fazer deploy**: `deployment.md`
- **Trocar cor/layout**: `design-system.md`
- **Planejar próximas fases**: `roadmap.md`

## Convenções desta documentação

- Código em blocos com linguagem (`csharp`, `typescript`, `bash`, `yaml`).
- Decisões importantes viram ADR (formato curto, imutável após aceito).
- Nomes de arquivo e caminhos sempre relativos à raiz do repo.
- Datas em `YYYY-MM-DD`.

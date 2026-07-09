# ADR 0001 — Escolha de stack

**Status:** aceito
**Data:** 2026-04-23

## Contexto

Precisamos escolher linguagem e framework de backend, frontend e plataforma de execução para o Utilix. Critérios:

- DX do time (conhecimento prévio)
- Custo de hosting baixo, ideal tier grátis com scale-to-zero
- Suporte a processos externos (FFmpeg, yt-dlp, Ghostscript)
- Suporte a WebSockets para progresso em tempo real
- Preparação para evolução (workers horizontais, auth, pagamento)

## Alternativas consideradas

### Backend

| Opção | Prós | Contras |
|---|---|---|
| **ASP.NET Core 10 (LTS)** | DX excelente, SignalR integrado, Minimal APIs maduras, AOT melhorado, LTS até nov/2028 | Imagem Docker maior que Go/Node |
| Node.js + Fastify | Imagem pequena, ecossistema web gigante | Single-threaded, menos confortável para engine orchestration |
| Go + Gin | Imagem mínima, goroutines perfeitas para fila | Time não conhece; menos produtivo em CRUDs |
| Python + FastAPI | Reaproveita código legado direto | GIL limita throughput; reuso parcial do legado não justifica |

### Frontend

| Opção | Prós | Contras |
|---|---|---|
| **Angular 21 (standalone + signals + zoneless)** | Signals estáveis, zoneless padrão (bundle menor), `@defer` maduro, DI funcional, estrutura forte | Curva de aprendizado; bundle ainda maior que Svelte |
| React + Next.js | SSR out-of-box, ecossistema enorme | Menos estrutura; decisões de arquitetura ficam com o time |
| Svelte + SvelteKit | Bundle minúsculo, DX ótima | Menos maduro para times maiores |

### Plataforma

| Opção | Prós | Contras |
|---|---|---|
| **Google Cloud Run** | Scale-to-zero, tier grátis generoso, 60 min timeout, aceita containers | Egress $0.12/GB (mitigado com R2) |
| Azure Container Apps | Scale-to-zero, tier grátis similar | Ecossistema GCP melhor para containers simples |
| AWS Fargate | Maduro | Sem scale-to-zero real; App Runner cobra fixo |
| AWS Lambda container | Cheapest se caber | 15 min timeout mata conversão de vídeo |
| Fly.io | DX ótima, scale-to-zero | Tier grátis menor |
| Hetzner VPS | Custo fixo barato (€6/mês), zero vendor lock | Precisa operar; sem scale-to-zero |

## Decisão

- **Backend:** ASP.NET Core 10 com Minimal APIs (LTS, nov/2025, suporte até nov/2028)
- **Frontend:** Angular 21 com standalone components, signals e zoneless change detection
- **Plataforma:** Google Cloud Run para API e Gotenberg; Cloudflare Pages para frontend; Cloudflare R2 para storage

Escolhemos as versões LTS mais recentes para maximizar vida útil do suporte e aproveitar melhorias de performance (AOT no .NET, zoneless no Angular).

## Justificativa

- **ASP.NET Core 10**: SignalR integrado elimina necessidade de biblioteca extra para progresso em tempo real. DI e BackgroundService dão a arquitetura certa para fila de jobs. Minimal APIs reduzem boilerplate (ver [ADR 0005](0005-minimal-apis-over-clean-arch.md)). Native AOT no .NET 10 reduz imagem do container se precisarmos.
- **Angular 21**: estrutura clara para crescer. CLI gera PWA automaticamente. Zoneless + signals removem sobrecarga do zone.js (bundle ~30% menor que Angular 17 em apps equivalentes). DI funcional (`inject()`) elimina classes desnecessárias.
- **Cloud Run**: único serviço que combina scale-to-zero + tier grátis generoso + timeout de 60 min + aceita qualquer container. Azure Container Apps seria empate técnico; escolhemos Cloud Run pela DX mais direta.
- **R2**: egress zero é decisivo. Um usuário baixando um vídeo de 500MB custaria ~$0.06 em S3/GCS; em R2, custa $0.
- **Pages**: frontend estático, CDN global, deploy via git push, 100% grátis para nosso volume esperado.

## Consequências

**Positivas:**
- Custo no ócio: $0.
- Custo em tráfego baixo: centavos/mês.
- Portabilidade: tudo containerizado, migrar para Hetzner se custo explodir é questão de `docker compose up`.
- DX forte em toda a stack.

**Negativas:**
- Vendor lock no GCP para deploy de API. Mitigado: Dockerfile é padrão, qualquer serverless de container roda.
- Cold start de Cloud Run (~1-3s no primeiro request após idle). Aceitável para conversão; usuário não percebe porque já está esperando alguns segundos de qualquer forma.
- Angular tem bundle maior que Svelte. Mitigado com lazy loading por feature.

## Revisão

Reavaliar em 3 meses se:
- Custo mensal passar de $30 consistente → migrar para Hetzner.
- Cold start virar queixa frequente → `min-instances=1` (custa ~$6/mês).
- Time crescer > 5 devs no frontend → avaliar se Angular continua fit.
- Sair versão LTS nova (.NET 12 em 2027) → planejar atualização no ciclo seguinte.

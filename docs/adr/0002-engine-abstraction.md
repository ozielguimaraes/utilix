# ADR 0002 — Abstração de engine como processos externos

**Status:** aceito
**Data:** 2026-04-23

## Contexto

Cada utilitário de conversão (YouTube, vídeo, áudio, imagem, PDF, documento) usa uma ferramenta diferente, muitas delas binários nativos (FFmpeg, yt-dlp, Ghostscript) ou bibliotecas com binding nativo (Magick.NET).

Precisamos de um padrão que:

1. Permita adicionar novos utilitários sem tocar em controller, queue, frontend ou outros engines.
2. Isole dependências — se FFmpeg der ruim, não derruba a API.
3. Tolere reescrita de implementação (ex: trocar Magick.NET por ImageSharp) sem quebrar consumidores.
4. Facilite teste unitário (mock do runner, não do binário).

## Alternativas consideradas

### A. Biblioteca por engine, chamada in-process

Cada engine usa SDK/biblioteca .NET.
- **Prós**: sem `Process.Start`, sem parsing de stderr, sem gerenciar timeout de processo.
- **Contras**: nem toda ferramenta tem biblioteca .NET decente (yt-dlp só tem CLI; LibreOffice idem). Misturar estilos piora consistência.

### B. Microsserviço por engine (gRPC/HTTP)

Cada engine é um processo independente exposto via API.
- **Prós**: isolamento total, escala independente, linguagem livre (poderia manter booklet em Python).
- **Contras**: complexidade operacional absurda para o MVP. 6 serviços rodando no Cloud Run = 6x o cold start, 6x a configuração.

### C. Interface única + processos externos via wrapper

Uma `IConversionEngine` para todos. Implementações chamam CLI via `IProcessRunner` (para FFmpeg/yt-dlp/Ghostscript), biblioteca .NET pura (para ImageSharp/QuestPDF), ou HTTP (para Gotenberg).
- **Prós**: consistência, fácil adicionar engine, testável via mock do runner.
- **Contras**: abstração pode vazar quando ferramentas têm APIs muito diferentes.

## Decisão

**Alternativa C.** Todos os engines implementam `IConversionEngine`. O mecanismo interno pode ser qualquer um (CLI, lib, HTTP) — é detalhe de implementação.

Para chamar binários, usar `IProcessRunner` (wrapper centralizado). Para Gotenberg, usar `HttpClient` via `IHttpClientFactory`. Para libs puras .NET, usar direto.

## Interface

```csharp
public interface IConversionEngine
{
    EngineMetadata Metadata { get; }
    Task<ConversionResult> ExecuteAsync(
        ConversionInput input,
        IProgress<ProgressReport> progress,
        CancellationToken ct);
}
```

## Justificativa

- **Unidade única de tratamento**: orchestrator, rate limit, queue e SignalR tratam todos os engines pelo mesmo contrato. Adicionar engine não muda infraestrutura.
- **Isolamento suficiente**: processo externo morre → engine joga exceção → orchestrator marca job como failed. API não cai.
- **Teste**: `FakeProcessRunner` permite testar engines sem os binários instalados na máquina de CI.
- **Evolução incremental**: começar com CLI para tudo, trocar por lib .NET quando aparecer motivo (ex: ImageSharp já é lib; FFmpeg nunca vai ser).

## Consequências

**Positivas:**
- Adicionar utilitário: 1 classe + 1 registro DI + 1 chave i18n. Pronto.
- Sandbox natural: processo filho morre com timeout sem arrastar a API.
- Documentação clara (ver `engines.md`).

**Negativas:**
- Parsing de stderr é frágil (formato do FFmpeg muda entre versões). Mitigado: testes de integração que pinam versão dos binários no container.
- Working directory por job consome disco temporariamente. Mitigado: orchestrator limpa sempre, cleanup service faz fallback.

## Não-objetivos

- **Não** estamos tentando suportar engines de terceiros (plugins de usuário). Se for necessário no futuro, a abstração já cabe — mas o modelo de distribuição/segurança é outra história.
- **Não** vamos rodar engine em processo separado (como sidecar do Cloud Run). Se um engine específico exigir isolamento maior (ex: virar gargalo), aí sim vira microsserviço próprio.

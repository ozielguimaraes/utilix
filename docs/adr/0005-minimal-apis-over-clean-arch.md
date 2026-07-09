# ADR 0005 — Minimal APIs com REPR em vez de Clean Architecture

**Status:** aceito
**Data:** 2026-04-23

## Contexto

Precisamos escolher como organizar o código de backend. O padrão comum na comunidade .NET hoje é **Clean Architecture** (às vezes combinada com **Vertical Slices**):

```
Utilix.Domain/             # Entities, Value Objects, rules
Utilix.Application/        # Use cases, commands, queries, handlers, MediatR
Utilix.Infrastructure/     # EF Core, storage, external APIs
Utilix.Api/                # Controllers ou Minimal APIs
```

Com MediatR para separar `IRequest<T>` + `IRequestHandler<TRequest, TResponse>`.

A pergunta é: **isso agrega valor real para o Utilix?**

## Análise do domínio

O domínio de negócio do Utilix é:

```csharp
public record Job(
    Guid Id,
    string EngineSlug,
    JobStatus Status,
    int Progress,
    string? OutputKey,
    DateTimeOffset CreatedAt);

public enum JobStatus { Pending, Processing, Completed, Failed, Cancelled }
```

**Regras de negócio:**
- Status só avança em um sentido (state machine trivial).
- Não dá para cancelar job completado.
- Quota? Não no MVP.
- Billing? Não no MVP.
- Autorização? Não no MVP.

**Onde a complexidade realmente mora:**
- Orquestração de processos externos (FFmpeg, yt-dlp, Gotenberg).
- Upload chunked e storage.
- Progresso via SignalR.
- Fila de jobs.

Nada disso é **regra de negócio**. É **infraestrutura** e **integração**.

## O problema de aplicar Clean Arch aqui

Para o endpoint "criar job de conversão", Clean Arch gera:

1. `CreateJobCommand : IRequest<CreateJobResponse>` — DTO de entrada
2. `CreateJobCommandHandler : IRequestHandler<...>` — um handler com 10 linhas
3. `CreateJobValidator : AbstractValidator<CreateJobCommand>` — validação
4. `Job` entidade no `Domain/`
5. `IJobRepository` interface no `Application/`
6. `JobRepository` implementação no `Infrastructure/`
7. `CreateJobEndpoint` no `Api/` que chama `mediator.Send(command)`

**7 arquivos, 4 projetos, para o que é essencialmente:**

```
recebe arquivo → valida MIME → salva em storage → enfileira → retorna 202
```

Isso não é arquitetura — é fricção. Cada mudança de contrato tem que atravessar 7 arquivos. A abstração não está protegendo nada que seja frágil.

## Alternativas consideradas

### A. Clean Architecture + MediatR

Como descrito acima.
- **Prós**: padrão conhecido; escala para domínios complexos; separação clara de responsabilidades.
- **Contras**: ~7 arquivos para endpoint trivial; MediatR vira obstáculo para debug; abstração protege o que não está sob ataque.

### B. Vertical Slices + MediatR

Agrupa por feature mas mantém handler/command/validator separados.
- **Prós**: melhor que Clean Arch; tudo num lugar.
- **Contras**: ainda MediatR; ainda 3-4 arquivos por endpoint simples.

### C. **REPR com Minimal APIs** (decisão)

**R**equest-**E**ndpoint-**R**esponse: cada endpoint é um arquivo contendo a request, a response e o handler. Usar `MapGroup` no `Program.cs` para rotas.

- **Prós**: leitura linear; 1 endpoint = 1 arquivo; zero framework de mediação; performance melhor (Minimal APIs > Controllers > MediatR).
- **Contras**: se o handler crescer muito, arquivo pode ficar grande. Mitigável extraindo função ou partial class.

### D. Apenas Controllers tradicionais

- **Prós**: familiar.
- **Contras**: mais boilerplate que Minimal APIs; atribuição de rota por atributos espalha config; model binding tem mais pegadinhas.

## Decisão

**REPR com Minimal APIs** (alternativa C).

Organização:

```
Utilix.Api/
├── Features/
│   ├── Jobs/
│   │   ├── CreateJob.cs        # 1 arquivo: request + response + handler + Map()
│   │   ├── GetJob.cs
│   │   ├── CancelJob.cs
│   │   ├── Job.cs              # record do domínio
│   │   └── JobStore.cs         # estado (dict concorrente ou DbSet)
│   ├── Engines/
│   │   └── ListEngines.cs
│   └── Files/
│       └── DownloadFile.cs
└── Program.cs                  # MapGroup + CreateJob.Map(group)
```

### Template do endpoint

```csharp
namespace Utilix.Api.Features.Jobs;

public record CreateJobRequest(string EngineSlug, Dictionary<string,string>? Options, string? Url);
public record CreateJobResponse(Guid JobId, string Status, DateTimeOffset CreatedAt);

public static class CreateJob
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
             .DisableAntiforgery()
             .WithName("CreateJob")
             .Produces<CreateJobResponse>(StatusCodes.Status202Accepted);

    private static async Task<Results<Accepted<CreateJobResponse>, ValidationProblem>> Handle(
        HttpRequest http,
        JobStore store,
        IJobQueue queue,
        EngineRegistry engines,
        IFileStorage storage,
        CancellationToken ct)
    {
        // 1. parse multipart ou JSON
        // 2. validar engineSlug existe
        // 3. validar arquivo (tamanho, MIME)
        // 4. upload para storage
        // 5. criar Job, adicionar no store
        // 6. enfileirar
        // 7. retornar Accepted
    }
}
```

### Program.cs

```csharp
var app = builder.Build();

var jobs = app.MapGroup("/api/jobs").RequireRateLimiting("default");
CreateJob.Map(jobs);
GetJob.Map(jobs);
CancelJob.Map(jobs);

var engines = app.MapGroup("/api/engines");
ListEngines.Map(engines);

var files = app.MapGroup("/api/files");
DownloadFile.Map(files);

app.MapHub<ConversionHub>("/hubs/conversion");
app.Run();
```

## Regra para evoluir

**Não reorganizar prematuramente.** Aplicar Clean Arch / Vertical Slices com MediatR **só quando surgir motivo real:**

- Regra de negócio que precisa ser testada isolada do HTTP.
- Múltiplas formas de disparar o mesmo use case (HTTP + background + scheduled).
- Domínio com invariantes complexas (agregados, eventos, sagas).

Enquanto Utilix for "recebe, converte, devolve", o endpoint **é** o use case.

## Consequências

**Positivas:**
- Leitura linear. Novo dev acha a lógica imediatamente.
- Menos arquivos, menos indireções.
- Performance: Minimal APIs > Controllers + MediatR.
- Sem dependência de biblioteca de mediação.
- Refatoração simples quando chegar a hora.

**Negativas:**
- Handler pode crescer. Se passar de ~100 linhas, extrair funções privadas no mesmo arquivo ou quebrar em partial class.
- Quem está acostumado com Clean Arch pode estranhar no início. Documentado aqui e em `folder-structure.md`.
- Se a regra de negócio crescer de verdade, vai ter refactor. Aceito o custo — fazer agora é overengineer; fazer depois é YAGNI ao avesso.

## Não-objetivos

- **Não** proibimos classes/services auxiliares. Se um handler precisa de lógica reutilizável, extrair para uma classe normal em `Infrastructure/` ou `Features/{Feature}/`. Só estamos evitando a **camada cerimonial** de Command/Handler/Validator separados.
- **Não** proibimos MediatR para sempre. Se um dia houver motivo real (múltiplos triggers para o mesmo use case), reavaliar.

## Revisão

Reavaliar se:
- Introduzirmos auth/quota/billing — aí surgem regras de negócio reais e refatorar para camadas passa a fazer sentido.
- Um feature crescer > 5 endpoints e começar a ter lógica compartilhada não-trivial.
- Precisarmos rodar o mesmo use case fora do HTTP (scheduler, fila externa, CLI).

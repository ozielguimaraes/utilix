# ADR 0004 — Storage em R2 com retenção de 1 hora

**Status:** aceito
**Data:** 2026-04-23

## Contexto

O Utilix precisa armazenar:

1. **Uploads do usuário** (fonte da conversão): `uploads/{jobId}/source.ext`
2. **Saídas da conversão** (o que o usuário baixa): `outputs/{jobId}/result.ext`

Critérios:

- **Custo**: armazenar vídeos de 500MB pode somar rápido. Queremos retenção curta.
- **Egress**: usuário baixando arquivo é caro em provedores padrão (S3 $0.09/GB, GCS $0.12/GB).
- **Privacidade**: arquivos não devem ficar guardados indefinidamente. Conversão é operação transitória.
- **Download direto**: queremos que o browser baixe direto do storage (URL assinada), sem passar pela API — economiza CPU e memória da API.

## Alternativas consideradas

### A. Filesystem local no Cloud Run

- **Prós**: grátis, zero latência.
- **Contras**: Cloud Run é efêmero — filesystem é apagado a cada scale-to-zero. Multi-instância não compartilha disco. Impossível emitir URL direta.

### B. AWS S3

- **Prós**: maduro, SDK .NET excelente.
- **Contras**: egress $0.09/GB. Usuário baixando 500MB custa $0.045. Em volume, vira o maior custo do produto.

### C. Google Cloud Storage

- **Prós**: integração natural com Cloud Run (IAM).
- **Contras**: egress $0.12/GB (pior que S3). Incompatível com a tese de custo baixo.

### D. Cloudflare R2

API compatível com S3.
- **Prós**: **egress zero**. Tier grátis de 10GB. 10M class-A ops/mês grátis.
- **Contras**: menos recursos que S3 (sem lifecycle transitions entre classes, sem versioning avançado). Para nosso caso, irrelevante.

### E. Backblaze B2

- **Prós**: $0.006/GB armazenado (barato); egress 3x o armazenado é grátis.
- **Contras**: egress após o limite é $0.01/GB. Pior que R2.

## Decisão

**Cloudflare R2** com política de retenção de **1 hora**.

- Upload: backend escreve em `uploads/{jobId}/source.ext`.
- Saída: backend escreve em `outputs/{jobId}/result.ext` e gera **URL pré-assinada** (S3-compatible, TTL 1h).
- Usuário baixa direto do R2 via URL assinada — sem passar pela API.
- `CleanupService` (HostedService) roda a cada 15 min e apaga objetos com `CreatedAt < now - 1h`.
- Defesa em profundidade: lifecycle rule no bucket apaga objetos com > 1 dia (caso o backend falhe).

## Justificativa

- **Egress zero**: transforma o maior custo variável em $0. Usuário pode baixar vídeo de 2GB sem impacto no custo operacional.
- **Privacidade por padrão**: 1h é tempo suficiente para o usuário baixar, curto o bastante para não funcionar como "armazenamento". Se ele quiser guardar, baixa e guarda localmente.
- **S3-compatible**: SDK `AWSSDK.S3` funciona direto. Zero lock-in — migrar para S3/GCS é só trocar endpoint e credenciais.
- **Custo previsível**: 10GB + 10M ops grátis cobrem dezenas de milhares de conversões mensais. Acima disso, $0.015/GB armazenado (já barato).

## Implementação

```csharp
public interface IFileStorage
{
    Task<string> UploadAsync(string key, Stream stream, string contentType, CancellationToken ct);
    Task<Stream> DownloadAsync(string key, CancellationToken ct);
    Task<string> GetSignedDownloadUrlAsync(string key, TimeSpan ttl, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
    IAsyncEnumerable<StorageObject> ListAsync(string prefix, CancellationToken ct);
}
```

Duas implementações:
- `R2FileStorage` (produção) — usa AWSSDK.S3 apontando para endpoint R2.
- `LocalFileStorage` (dev) — escreve em `./data/`.

`CleanupService`:
```csharp
public class CleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var cutoff = DateTimeOffset.UtcNow.AddHours(-1);
            await foreach (var obj in _storage.ListAsync("", ct))
            {
                if (obj.LastModified < cutoff)
                    await _storage.DeleteAsync(obj.Key, ct);
            }
            await Task.Delay(TimeSpan.FromMinutes(15), ct);
        }
    }
}
```

## Consequências

**Positivas:**
- Custo de egress = $0 mesmo com volume alto de downloads grandes.
- Privacidade: arquivos somem sozinhos.
- Portabilidade: trocar R2 por S3 é uma linha de config.

**Negativas:**
- Usuário que fica 1h sem baixar perde o resultado. Mitigado: UX mostra `expiresAt` e botão "Baixar" em destaque assim que o job completa.
- URLs assinadas expõem levemente a estrutura de storage (nome do bucket, key). Aceitável — chave é GUID random.
- Lifecycle rule de 1 dia como fallback custa ops (DELETE). Irrelevante no tier grátis.

## Revisão

Reavaliar se:
- Usuários reclamarem de 1h curto demais → considerar 6h ou 24h com trade-off de custo.
- Custo de armazenamento (não egress) crescer — lifecycle deletar mais agressivo.
- Precisarmos de CDN na frente do R2 para múltiplas regiões → adicionar Cloudflare CDN (custo ainda $0 de egress).

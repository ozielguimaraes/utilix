# API

## Base

- **Produção**: `https://api.utilix.app` (placeholder)
- **Desenvolvimento**: `http://localhost:5000`
- Content-Type padrão: `application/json`
- Upload: `multipart/form-data`
- Autenticação: nenhuma no MVP. Rate limit por IP.

## Endpoints REST

### `GET /api/engines`

Lista os engines disponíveis. Frontend usa para montar catálogo.

**Response 200:**
```json
[
  {
    "slug": "youtube",
    "displayNameKey": "engines.youtube.name",
    "category": "media",
    "acceptedInputs": ["url"],
    "outputFormats": ["mp4", "mp3"],
    "options": [
      {
        "key": "format",
        "type": "select",
        "defaultValue": "video",
        "choices": ["video", "audio"]
      },
      {
        "key": "quality",
        "type": "select",
        "defaultValue": "best",
        "choices": ["best", "1080p", "720p", "480p"]
      }
    ],
    "maxInputSizeBytes": 0
  },
  {
    "slug": "video",
    "displayNameKey": "engines.video.name",
    "category": "media",
    "acceptedInputs": ["video/mp4", "video/quicktime", "video/webm"],
    "outputFormats": ["mp4", "webm", "gif"],
    "options": [...],
    "maxInputSizeBytes": 524288000
  }
]
```

### `POST /api/jobs`

Cria um job de conversão. Aceita `multipart/form-data` (upload) ou `application/json` (URL-based).

**Request (multipart):**
```
POST /api/jobs
Content-Type: multipart/form-data

engineSlug=video
file=@my-video.mov
options={"outputFormat":"mp4","quality":"720p"}
```

**Request (json, para YouTube):**
```json
POST /api/jobs
{
  "engineSlug": "youtube",
  "url": "https://youtube.com/watch?v=...",
  "options": {
    "format": "video",
    "quality": "720p"
  }
}
```

**Response 202 Accepted:**
```json
{
  "jobId": "a1b2c3d4-...",
  "status": "pending",
  "createdAt": "2026-04-23T14:30:00Z",
  "signalRGroup": "job:a1b2c3d4-..."
}
```

**Erros:**
- `400` — engineSlug inválido, options malformadas, arquivo acima do limite
- `413` — arquivo muito grande
- `415` — MIME type não aceito pelo engine
- `429` — rate limit excedido

### `GET /api/jobs/{id}`

Consulta estado de um job. Usado como **fallback** quando SignalR não conecta.

**Response 200:**
```json
{
  "jobId": "a1b2c3d4-...",
  "engineSlug": "video",
  "status": "processing",
  "progress": {
    "percent": 42,
    "stage": "converting",
    "message": "Codificando vídeo..."
  },
  "createdAt": "2026-04-23T14:30:00Z",
  "startedAt": "2026-04-23T14:30:02Z",
  "completedAt": null,
  "result": null,
  "error": null
}
```

**Status possíveis:** `pending` | `processing` | `completed` | `failed` | `cancelled`

**Ao completar:**
```json
{
  "status": "completed",
  "completedAt": "2026-04-23T14:32:15Z",
  "result": {
    "fileName": "my-video.mp4",
    "mimeType": "video/mp4",
    "sizeBytes": 12345678,
    "downloadUrl": "https://r2.utilix.app/outputs/.../my-video.mp4?X-Amz-...",
    "expiresAt": "2026-04-23T15:32:15Z"
  }
}
```

**Ao falhar:**
```json
{
  "status": "failed",
  "error": {
    "code": "ENGINE_EXECUTION_FAILED",
    "messageKey": "errors.engine.unsupported_codec",
    "retryable": false
  }
}
```

### `DELETE /api/jobs/{id}`

Cancela um job em andamento. Se já completou, retorna 409.

**Response 204** (sem body)

### `GET /api/files/{fileId}`

Proxy de download (alternativa à URL assinada). Útil para forçar download com nome amigável.

**Response 200:** stream do arquivo com `Content-Disposition: attachment`.

Em produção, preferir URL assinada direta do R2 (zero-cost egress, sem passar pelo backend).

## SignalR

### Hub: `/hubs/conversion`

Cliente Angular:

```ts
const conn = new HubConnectionBuilder()
  .withUrl(`${env.apiUrl}/hubs/conversion`)
  .withAutomaticReconnect()
  .build();

await conn.start();
await conn.invoke('SubscribeToJob', jobId);

conn.on('progress', (report: ProgressReport) => { ... });
conn.on('completed', (result: JobResult) => { ... });
conn.on('failed', (error: JobError) => { ... });
```

### Métodos do Hub (cliente → servidor)

| Método | Args | Efeito |
|---|---|---|
| `SubscribeToJob` | `jobId: string` | Entra no grupo `job:{jobId}` |
| `UnsubscribeFromJob` | `jobId: string` | Sai do grupo |

### Eventos (servidor → cliente)

**`progress`**
```json
{
  "jobId": "...",
  "percent": 42,
  "stage": "converting",
  "message": "Codificando vídeo..."
}
```
Emitido conforme engine reporta. Para evitar flood, orquestrador faz throttle (máx 4/s por job).

**`completed`**
```json
{
  "jobId": "...",
  "fileName": "my-video.mp4",
  "downloadUrl": "...",
  "expiresAt": "2026-04-23T15:32:15Z",
  "sizeBytes": 12345678
}
```

**`failed`**
```json
{
  "jobId": "...",
  "code": "ENGINE_EXECUTION_FAILED",
  "messageKey": "errors.engine.unsupported_codec",
  "retryable": false
}
```

## Modelo de erro

Todos os erros REST retornam:

```json
{
  "code": "INVALID_ENGINE_SLUG",
  "messageKey": "errors.api.invalid_engine_slug",
  "details": { "slug": "xyz" },
  "traceId": "..."
}
```

`messageKey` é resolvido pelo frontend via i18n.

## Exemplos cURL

**Conversão de vídeo:**
```bash
curl -X POST https://api.utilix.app/api/jobs \
  -F "engineSlug=video" \
  -F "file=@video.mov" \
  -F 'options={"outputFormat":"mp4","quality":"720p"}'
```

**YouTube:**
```bash
curl -X POST https://api.utilix.app/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "engineSlug": "youtube",
    "url": "https://youtube.com/watch?v=...",
    "options": {"format":"audio"}
  }'
```

**Polling:**
```bash
curl https://api.utilix.app/api/jobs/a1b2c3d4-...
```

## Rate limiting

- **10 jobs / minuto** por IP (MVP).
- Header de resposta: `X-RateLimit-Remaining: 7`, `X-RateLimit-Reset: 1714234567`.
- 429 quando excedido.

## CORS

- Em produção, origem liberada: `https://utilix.app`.
- Em desenvolvimento: `http://localhost:4200`.

## OpenAPI

API expõe `GET /swagger/v1/swagger.json`. Em CI, geramos TypeScript types em `packages/shared-types/` via `openapi-typescript`.

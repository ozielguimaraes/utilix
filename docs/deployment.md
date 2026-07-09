# Deployment

Estratégia: **scale-to-zero em tudo que é possível**, **storage com egress zero**, **portabilidade garantida via Docker**.

## Topologia

```
Cloudflare Pages ─── Angular PWA (estático)
         │
         │ HTTPS
         ▼
Google Cloud Run ──► Utilix.Api (container .NET 10)
         │                  │
         │                  ├─── chama ────► Gotenberg (outro Cloud Run)
         │                  │
         │                  └─── R/W ──────► Cloudflare R2
         │
         └──── SignalR WebSocket (mesmo Cloud Run)
```

## Custo estimado

| Serviço | Tier grátis | Custo esperado baixo tráfego | Custo 10k conversões/mês |
|---|---|---|---|
| Cloudflare Pages | Ilimitado request, 500 builds/mês | $0 | $0 |
| Cloud Run API | 2M req, 180k vCPU-s, 360k GiB-s | $0 | ~$5–15 |
| Cloud Run Gotenberg | mesmo tier compartilhado | $0 | ~$2–5 (uso esporádico) |
| Cloudflare R2 | 10 GB + 10M class-A ops/mês | $0 | ~$0–3 |
| Cloudflare Pages Functions (se usarmos) | 100k req/dia | $0 | $0 |
| **Total** | — | **$0** | **~$7–23/mês** |

Quando passar de ~$30/mês consistentes, migrar para **Hetzner CX22** (€6/mês, 2 vCPU, 4GB RAM, 40GB SSD) + Docker Compose. O `Dockerfile.api` roda sem mudanças.

## Pré-requisitos

- Conta Google Cloud com billing ativado (obrigatório mesmo no tier grátis).
- Conta Cloudflare com Pages + R2.
- `gcloud` CLI, `wrangler` CLI, Docker.

## Passo a passo

### 1. Cloudflare R2 — storage

1. Criar bucket `utilix-prod`.
2. Gerar API token R2 com permissões `Object Read & Write` no bucket.
3. Configurar CORS para permitir origem do frontend:

```json
[
  {
    "AllowedOrigins": ["https://utilix.app"],
    "AllowedMethods": ["GET", "PUT", "POST"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3600
  }
]
```

4. Configurar lifecycle rule: deletar objetos após 1 dia (defesa em profundidade; backend também limpa ativamente).

Guardar credenciais para injetar como env vars no Cloud Run:
- `R2_ACCESS_KEY_ID`
- `R2_SECRET_ACCESS_KEY`
- `R2_ACCOUNT_ID`
- `R2_BUCKET=utilix-prod`
- `R2_ENDPOINT=https://{account-id}.r2.cloudflarestorage.com`

### 2. Gotenberg — Cloud Run

```bash
gcloud run deploy gotenberg \
  --image=gotenberg/gotenberg:8 \
  --region=us-central1 \
  --platform=managed \
  --memory=1Gi \
  --cpu=1 \
  --min-instances=0 \
  --max-instances=3 \
  --timeout=300 \
  --no-allow-unauthenticated \
  --port=3000
```

Depois, dar permissão para o service account da API invocar:

```bash
gcloud run services add-iam-policy-binding gotenberg \
  --member="serviceAccount:utilix-api@PROJECT.iam.gserviceaccount.com" \
  --role="roles/run.invoker" \
  --region=us-central1
```

Anotar a URL: `https://gotenberg-xxx-uc.a.run.app`. Vai como env var `GOTENBERG_URL` na API.

### 3. Utilix.Api — Cloud Run

**`docker/Dockerfile.api`:**

```dockerfile
# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY apps/api/. .
RUN dotnet publish Utilix.Api/Utilix.Api.csproj -c Release -o /app

# Stage 2: runtime com binários
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends \
    ffmpeg \
    ghostscript \
    curl \
    python3 \
    python3-pip \
    && pip3 install --no-cache-dir --break-system-packages yt-dlp \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Utilix.Api.dll"]
```

**Build e deploy:**

```bash
# Build local (ou via Cloud Build)
docker build -f docker/Dockerfile.api -t gcr.io/PROJECT/utilix-api:latest .
docker push gcr.io/PROJECT/utilix-api:latest

# Deploy
gcloud run deploy utilix-api \
  --image=gcr.io/PROJECT/utilix-api:latest \
  --region=us-central1 \
  --platform=managed \
  --memory=2Gi \
  --cpu=2 \
  --min-instances=0 \
  --max-instances=5 \
  --timeout=600 \
  --allow-unauthenticated \
  --set-env-vars="R2_ACCOUNT_ID=...,R2_BUCKET=utilix-prod,R2_ENDPOINT=...,GOTENBERG_URL=https://gotenberg-xxx.run.app" \
  --set-secrets="R2_ACCESS_KEY_ID=r2-key:latest,R2_SECRET_ACCESS_KEY=r2-secret:latest" \
  --service-account=utilix-api@PROJECT.iam.gserviceaccount.com
```

Secrets no Secret Manager:

```bash
echo -n "KEY_ID" | gcloud secrets create r2-key --data-file=-
echo -n "SECRET" | gcloud secrets create r2-secret --data-file=-
```

Dar acesso:

```bash
gcloud secrets add-iam-policy-binding r2-key \
  --member="serviceAccount:utilix-api@PROJECT.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"
```

### 4. Angular PWA — Cloudflare Pages

Conectar repositório no dashboard Pages com:

- Framework preset: `Angular`
- Build command: `cd apps/web && npm ci && npm run build -- --configuration=production`
- Build output: `apps/web/dist/web/browser`
- Env var: `API_URL=https://utilix-api-xxx-uc.a.run.app`

Domínio custom: `utilix.app` via Cloudflare DNS (CNAME para o subdomínio pages.dev).

### 5. CI/CD

**`.github/workflows/api-deploy.yml`** (trigger em push na main):

```yaml
name: API deploy
on:
  push:
    branches: [main]
    paths: ['apps/api/**', 'docker/Dockerfile.api']
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: google-github-actions/auth@v2
        with:
          credentials_json: ${{ secrets.GCP_SA_KEY }}
      - uses: google-github-actions/setup-gcloud@v2
      - run: gcloud auth configure-docker
      - run: docker build -f docker/Dockerfile.api -t gcr.io/${{ secrets.GCP_PROJECT }}/utilix-api:${{ github.sha }} .
      - run: docker push gcr.io/${{ secrets.GCP_PROJECT }}/utilix-api:${{ github.sha }}
      - run: |
          gcloud run deploy utilix-api \
            --image=gcr.io/${{ secrets.GCP_PROJECT }}/utilix-api:${{ github.sha }} \
            --region=us-central1
```

Cloudflare Pages tem CI nativo — sem workflow necessário.

## Desenvolvimento local

**`docker/docker-compose.yml`:**

```yaml
services:
  api:
    build:
      context: ..
      dockerfile: docker/Dockerfile.api
    ports: ["5000:8080"]
    environment:
      - R2_ENDPOINT=http://minio:9000
      - R2_BUCKET=utilix-dev
      - R2_ACCESS_KEY_ID=minioadmin
      - R2_SECRET_ACCESS_KEY=minioadmin
      - GOTENBERG_URL=http://gotenberg:3000
    depends_on: [gotenberg, minio]

  gotenberg:
    image: gotenberg/gotenberg:8
    ports: ["3000:3000"]

  minio:
    image: minio/minio
    command: server /data --console-address ":9001"
    ports: ["9000:9000", "9001:9001"]
    environment:
      - MINIO_ROOT_USER=minioadmin
      - MINIO_ROOT_PASSWORD=minioadmin
```

Frontend rodando separado:

```bash
cd apps/web && npm run start  # http://localhost:4200
```

## Monitoramento

- **Cloud Run**: Logs e métricas nativas no Cloud Logging. Alertas via Cloud Monitoring (CPU > 80%, erros 5xx > 1%).
- **Cloudflare R2**: dashboard com uso de storage e operações.
- **Cloudflare Pages**: analytics básico incluído.

## Plano B — Hetzner

Se custo passar de $30/mês:

1. `docker-compose.yml` idêntico ao de dev, apontando R2 real.
2. Hetzner CX22 (€6/mês): `docker compose up -d`.
3. Caddy ou Traefik na frente para TLS automático.
4. Apontar DNS da API para o IP do VPS.
5. Frontend continua no Cloudflare Pages.

Zero mudança de código. Abstração `IFileStorage` segue apontando para R2.

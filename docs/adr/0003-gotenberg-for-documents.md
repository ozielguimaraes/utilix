# ADR 0003 — Gotenberg serverless para documentos

**Status:** aceito
**Data:** 2026-04-23

## Contexto

Precisamos converter documentos de escritório (docx, pptx, xlsx, odt, ods, odp) para PDF. A ferramenta padrão é LibreOffice em modo headless.

Problemas de embutir LibreOffice no container principal:

- **Tamanho da imagem**: LibreOffice + dependências adiciona ~1.5GB. Tempo de cold start sobe de ~1s para ~10s.
- **Uso esporádico**: maioria das conversões no Utilix não é de documento. Manter LibreOffice carregado sempre é desperdício.
- **Custo**: Cloud Run cobra por vCPU-second mesmo ocioso durante um request. Container grande = mais memória reservada = mais caro.
- **Complexidade**: chamar soffice via `Process.Start` exige gerenciar profile, lock files e timeouts específicos.

## Alternativas consideradas

### A. LibreOffice dentro do container principal

- **Prós**: zero network hop, tudo num deploy só.
- **Contras**: +1.5GB, cold start longo, uso esporádico desperdiça memória.

### B. Aspose.Words / Aspose.Cells (.NET)

Bibliotecas .NET comerciais de alta fidelidade.
- **Prós**: in-process, sem binário externo.
- **Contras**: licença paga (~$1000+/dev/ano). Incompatível com o objetivo de custo zero.

### C. Apenas Puppeteer/Playwright para html → pdf

Funciona para markdown/html mas não converte docx/pptx de verdade.
- **Prós**: leve.
- **Contras**: escopo pequeno demais. Não cumpre o requisito.

### D. Gotenberg em container separado (serverless)

[Gotenberg](https://gotenberg.dev) é um wrapper Docker oficial do LibreOffice + Chromium que expõe API HTTP.
- **Prós**: imagem pronta e mantida; API HTTP limpa; roda em Cloud Run com scale-to-zero; só gasta recurso quando alguém converte documento.
- **Contras**: network hop (latência +200-500ms); requer autenticação entre serviços; gerencia deploy de 2 serviços.

### E. API paga (CloudConvert, PDF.co, etc.)

- **Prós**: zero operação.
- **Contras**: $0.005-0.01 por conversão. Dependência externa que pode sumir ou subir preço.

## Decisão

**Alternativa D.** Gotenberg em Cloud Run separado. A API principal chama via `HttpClient` autenticado (IAM do Cloud Run). Scale-to-zero independente: quando ninguém converte documento, custa $0.

## Justificativa

- **Custo**: imagem principal fica enxuta; Gotenberg só sobe quando precisa.
- **Operacional**: imagem oficial `gotenberg/gotenberg:8`, zero manutenção de nossa parte.
- **Isolamento**: se LibreOffice travar, só derruba o Gotenberg. API principal continua servindo outros engines.
- **Portabilidade**: Gotenberg é Docker puro. Se migrarmos para Hetzner, roda no mesmo docker-compose.
- **Fidelidade**: LibreOffice converte docx/pptx melhor que qualquer alternativa leve. Aspose seria melhor ainda mas inviável pelo custo.

## Implementação

```csharp
public class DocumentEngine : IConversionEngine
{
    private readonly HttpClient _http;

    public async Task<ConversionResult> ExecuteAsync(...)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(File.OpenRead(sourcePath)),
                    "files", input.SourceFileName!);

        var response = await _http.PostAsync(
            "/forms/libreoffice/convert", content, ct);
        response.EnsureSuccessStatusCode();

        var outputPath = Path.Combine(input.WorkingDirectory, "result.pdf");
        await using var fs = File.Create(outputPath);
        await response.Content.CopyToAsync(fs, ct);

        return new ConversionResult(
            outputPath, "result.pdf", "application/pdf",
            new FileInfo(outputPath).Length);
    }
}
```

`HttpClient` configurado em `Program.cs` com base address vindo de `GOTENBERG_URL`, timeout de 5 min, e autenticação IAM do Cloud Run.

## Consequências

**Positivas:**
- Container da API cai para ~300MB.
- Cold start da API reduz drasticamente.
- Custo de documento é pay-per-use real.

**Negativas:**
- +1 serviço para deployar (mitigado por CI simples).
- Latência ligeiramente maior em conversões de documento (~300ms extra). Aceitável porque conversão de docx já leva 1-3s por si só.
- Precisa configurar IAM entre serviços. Documentado em `deployment.md`.

## Revisão

Reavaliar se:
- Gotenberg virar gargalo de custo (improvável — uso esporádico).
- Surgir biblioteca .NET open-source boa para docx→pdf.
- Volume de documentos superar vídeos (aí LibreOffice embutido passa a fazer sentido).

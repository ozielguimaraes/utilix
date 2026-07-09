using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Utilix.Abstractions.Process;
using Utilix.Tests.Infrastructure;

namespace Utilix.Tests.Features.Jobs;

public class JobsFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JobsFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var processRunnerDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType.Name.Contains("ProcessRunner"));
                if (processRunnerDescriptor != null)
                    services.Remove(processRunnerDescriptor);

                services.AddSingleton<IProcessRunner, FakeProcessRunner>();
            });
        });
    }

    [Fact]
    public async Task Cria_job_youtube_e_conclui_com_resultado()
    {
        var client = _factory.CreateClient();

        var createRequest = new
        {
            engineSlug = "youtube",
            url = "https://youtube.com/watch?v=abc123",
            options = new { format = "video", quality = "720p" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/jobs", createRequest);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, createResponse.StatusCode);

        var createdJson = await createResponse.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(createdJson);
        var jobIdString = doc.RootElement.GetProperty("jobId").GetString();
        Assert.NotNull(jobIdString);
        var jobId = Guid.Parse(jobIdString!);

        await Task.Delay(1000);

        var maxAttempts = 50;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var statusResponse = await client.GetAsync($"/api/jobs/{jobId}");
            Assert.True(statusResponse.IsSuccessStatusCode);

            var jobJson = await statusResponse.Content.ReadAsStringAsync();
            using var statusDoc = System.Text.Json.JsonDocument.Parse(jobJson);
            var statusRoot = statusDoc.RootElement;
            var status = statusRoot.GetProperty("status").GetString();

            if (status == "completed")
            {
                Assert.True(statusRoot.TryGetProperty("result", out var result));
                Assert.NotEqual(System.Text.Json.JsonValueKind.Null, result.ValueKind);
                var fileName = result.GetProperty("fileName").GetString();
                Assert.NotNull(fileName);
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Job não completou no tempo esperado");
    }

    [Fact]
    public async Task Rejeita_url_invalida()
    {
        var client = _factory.CreateClient();

        var createRequest = new
        {
            engineSlug = "youtube",
            url = "not-a-valid-url",
            options = new { format = "video", quality = "720p" }
        };

        var createResponse = await client.PostAsJsonAsync("/api/jobs", createRequest);
        Assert.Equal(System.Net.HttpStatusCode.Accepted, createResponse.StatusCode);

        var createdJson = await createResponse.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(createdJson);
        var root = doc.RootElement;
        var jobIdString = root.GetProperty("jobId").GetString();
        Assert.NotNull(jobIdString);

        await Task.Delay(500);

        var statusResponse = await client.GetAsync($"/api/jobs/{jobIdString}");
        var jobJson = await statusResponse.Content.ReadAsStringAsync();
        using var statusDoc = System.Text.Json.JsonDocument.Parse(jobJson);
        var statusRoot = statusDoc.RootElement;
        var status = statusRoot.GetProperty("status").GetString();

        Assert.Equal("failed", status);
        Assert.True(statusRoot.TryGetProperty("error", out var error));
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, error.ValueKind);
    }
}

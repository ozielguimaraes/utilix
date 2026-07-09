using Utilix.Abstractions.Engines;
using Utilix.Abstractions.Jobs;
using Utilix.Abstractions.Process;
using Utilix.Abstractions.Storage;
using Utilix.Api.Engines;
using Utilix.Api.Features.Engines;
using Utilix.Api.Features.Jobs;
using Utilix.Api.Hubs;
using Utilix.Api.Infrastructure.Process;
using Utilix.Api.Infrastructure.Queue;
using Utilix.Api.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<JobStore>();
builder.Services.AddSingleton<IJobQueue, ChannelJobQueue>();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<IConversionEngine, YoutubeEngine>();
builder.Services.AddSingleton<EngineRegistry>();
builder.Services.AddHostedService<JobOrchestrator>();
builder.Services.AddHostedService<StorageCleanupService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var jobsGroup = app.MapGroup("/api/jobs");
CreateJob.Map(jobsGroup);
GetJob.Map(jobsGroup);
DownloadJob.Map(jobsGroup);

var enginesGroup = app.MapGroup("/api/engines");
ListEngines.Map(enginesGroup);

app.MapHub<ConversionHub>("/api/hubs/conversion");

app.Run();

public partial class Program;

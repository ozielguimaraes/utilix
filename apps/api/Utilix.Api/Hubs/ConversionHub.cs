using Microsoft.AspNetCore.SignalR;
using Utilix.Abstractions.Engines;

namespace Utilix.Api.Hubs;

public sealed class ConversionHub : Hub
{
    private const string JobGroupPrefix = "job:";

    public async Task SubscribeToJob(string jobId)
    {
        if (!Guid.TryParse(jobId, out _))
        {
            await Clients.Caller.SendAsync("error", "jobId inválido");
            return;
        }

        var group = $"{JobGroupPrefix}{jobId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public async Task UnsubscribeFromJob(string jobId)
    {
        if (!Guid.TryParse(jobId, out _))
            return;

        var group = $"{JobGroupPrefix}{jobId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    public static async Task BroadcastProgress(IHubContext<ConversionHub> hubContext, Guid jobId, ProgressReport progress)
    {
        var group = $"{JobGroupPrefix}{jobId}";
        await hubContext.Clients.Group(group).SendAsync("progress",
            new { percent = progress.Percent, stage = progress.Stage, message = progress.Message });
    }

    public static async Task BroadcastCompleted(IHubContext<ConversionHub> hubContext, Guid jobId)
    {
        var group = $"{JobGroupPrefix}{jobId}";
        await hubContext.Clients.Group(group).SendAsync("completed");
    }

    public static async Task BroadcastFailed(IHubContext<ConversionHub> hubContext, Guid jobId, string errorMessageKey)
    {
        var group = $"{JobGroupPrefix}{jobId}";
        await hubContext.Clients.Group(group).SendAsync("failed", new { errorMessageKey });
    }
}

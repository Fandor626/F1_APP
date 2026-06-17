using Microsoft.AspNetCore.SignalR;

namespace F1App.Api.Hubs;

public class RaceHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "race");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "race");
        await base.OnDisconnectedAsync(exception);
    }
}

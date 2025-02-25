using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace streaming_dotnet.Hubs;

public class TestHub : Hub 
{
    private readonly UserService _userService;

    public TestHub(UserService service)
    {
        _userService = service;
    }

    public override Task OnConnectedAsync()
    {
        string? user = Context.GetHttpContext().Request.Headers["id"];
        System.Console.WriteLine(user);
        if (!string.IsNullOrEmpty(user)) 
        {
            _userService.AddUser(user, Context.ConnectionId);
        }
        return base.OnConnectedAsync();

    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _userService.RemoveConnectionId(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("test", user, message);
    }
}

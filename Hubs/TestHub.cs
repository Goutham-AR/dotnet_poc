using Microsoft.AspNetCore.SignalR;

namespace streaming_dotnet.Hubs;

public class TestHub : Hub 
{
    private readonly UserService _userService;

    public TestHub(UserService service)
    {
        _userService = service;
    }

    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("test", user, message);
    }
}

using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using streaming_dotnet.Hubs;

public class UserService
{
    private readonly IHubContext<TestHub> _hubContext;
    private readonly ConcurrentDictionary<string, string> _users = new();

    public UserService(IHubContext<TestHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public bool AddUser(string userId, string connectionId)
    {
        return _users.TryAdd(userId, connectionId);
    }

    public bool RemoveUser(string userId)
    {
        return _users.TryRemove(userId, out _);
    }

    public void RemoveConnectionId(string connectionId)
    {
        var key = _users.FirstOrDefault(elem => elem.Value == connectionId).Key;
        RemoveUser(key);
    }

    public string? GetConnectionId(string userId)
    {
        if (_users.TryGetValue(userId, out string? connectionId))
        {
            return connectionId;
        }
        return null;
    }
}

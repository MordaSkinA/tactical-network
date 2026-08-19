using System.Collections.Concurrent;
using GvGPoc.Models;

namespace GvGPoc.State;

public record SessionInfo(string Username, UserRole Role, string? SquadId);

public interface ISessionStore
{
    string CreateSession(SessionInfo info);
    SessionInfo? Get(string token);
    void Remove(string token);
}

// In-memory — рестарт сервера разлогинивает всех. Приемлемо для гильдийного
// инструмента такого масштаба.
public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();

    public string CreateSession(SessionInfo info)
    {
        var token = Guid.NewGuid().ToString("N");
        _sessions[token] = info;
        return token;
    }

    public SessionInfo? Get(string token) => _sessions.TryGetValue(token, out var info) ? info : null;

    public void Remove(string token) => _sessions.TryRemove(token, out _);
}
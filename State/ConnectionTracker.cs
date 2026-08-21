using System.Collections.Concurrent;

namespace GvGPoc.State;

// Следит за связью между пользователями и их соединениями SignalR
public interface IConnectionTracker
{
    void Add(string username, string connectionId);
    string? Remove(string connectionId);
    IReadOnlyCollection<string> GetConnections(string username);
}

public class InMemoryConnectionTracker : IConnectionTracker
{
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userToConnections =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string username, string connectionId)
    {
        _connectionToUser[connectionId] = username;
        lock (_userToConnections)
        {
            if (!_userToConnections.TryGetValue(username, out var set))
            {
                set = new HashSet<string>();
                _userToConnections[username] = set;
            }
            set.Add(connectionId);
        }
    }

    public string? Remove(string connectionId)
    {
        if (!_connectionToUser.TryRemove(connectionId, out var username))
            return null;

        lock (_userToConnections)
        {
            if (_userToConnections.TryGetValue(username, out var set))
            {
                set.Remove(connectionId);
                if (set.Count == 0)
                    _userToConnections.TryRemove(username, out _);
            }
        }
        return username;
    }

    public IReadOnlyCollection<string> GetConnections(string username)
    {
        lock (_userToConnections)
        {
            return _userToConnections.TryGetValue(username, out var set)
                ? set.ToArray()
                : Array.Empty<string>();
        }
    }
}

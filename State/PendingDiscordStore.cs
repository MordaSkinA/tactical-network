using System.Collections.Concurrent;

namespace GvGPoc.State;

public record PendingDiscordRegistration(string DiscordId, string DiscordUsername, DateTimeOffset ExpiresAt);

public interface IPendingDiscordStore
{
    string Create(string discordId, string discordUsername);
    PendingDiscordRegistration? Get(string token);
    void Remove(string token);
}


public class InMemoryPendingDiscordStore : IPendingDiscordStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, PendingDiscordRegistration> _pending = new();

    public string Create(string discordId, string discordUsername)
    {
        var token = Guid.NewGuid().ToString("N");
        _pending[token] = new PendingDiscordRegistration(discordId, discordUsername, DateTimeOffset.UtcNow.Add(Ttl));
        return token;
    }

    public PendingDiscordRegistration? Get(string token)
    {
        if (!_pending.TryGetValue(token, out var reg)) return null;
        if (reg.ExpiresAt < DateTimeOffset.UtcNow)
        {
            _pending.TryRemove(token, out _);
            return null;
        }
        return reg;
    }

    public void Remove(string token) => _pending.TryRemove(token, out _);
}

using System.Collections.Concurrent;

namespace GvGPoc.State;

public class SimpleRateLimiter
{
    private readonly int _maxHits;
    private readonly TimeSpan _window;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _hits = new();

    protected SimpleRateLimiter(int maxHits, TimeSpan window)
    {
        _maxHits = maxHits;
        _window = window;
    }

    public bool Allow(string key)
    {
        var now = DateTime.UtcNow;
        var queue = _hits.GetOrAdd(key, _ => new Queue<DateTime>());
        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > _window) queue.Dequeue();
            if (queue.Count >= _maxHits) return false;
            queue.Enqueue(now);
            return true;
        }
    }

    public void Reset(string key) => _hits.TryRemove(key, out _);
}

// Max 5 actions per 3 seconds from a single connection
public class HubActionRateLimiter : SimpleRateLimiter
{
    public HubActionRateLimiter() : base(maxHits: 5, window: TimeSpan.FromSeconds(3)) { }
}

// Max 5 login attempts per 30 seconds from a single IP
public class LoginAttemptRateLimiter : SimpleRateLimiter
{
    public LoginAttemptRateLimiter() : base(maxHits: 5, window: TimeSpan.FromSeconds(30)) { }
}
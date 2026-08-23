using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GvGPoc.Models;

namespace GvGPoc.State;

public record SessionInfo(string Username, UserRole Role, string? SquadId);

public interface ISessionStore
{
    string CreateSession(SessionInfo info);
    SessionInfo? Get(string token);
    void Remove(string token);
}


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


public record SessionTokenOptions(byte[] SigningKey, TimeSpan Ttl);


public class SignedSessionStore : ISessionStore
{
    private readonly byte[] _key;
    private readonly TimeSpan _ttl;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _revoked = new();

    public SignedSessionStore(SessionTokenOptions options)
    {
        _key = options.SigningKey;
        _ttl = options.Ttl;
    }

    private record Payload(string Jti, string Username, UserRole Role, string? SquadId, long Exp);

    public string CreateSession(SessionInfo info)
    {
        CleanupRevoked();

        var payload = new Payload(
            Jti: Guid.NewGuid().ToString("N"),
            Username: info.Username,
            Role: info.Role,
            SquadId: info.SquadId,
            Exp: DateTimeOffset.UtcNow.Add(_ttl).ToUnixTimeSeconds());

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var payloadPart = Base64UrlEncode(payloadBytes);
        var signaturePart = Base64UrlEncode(Sign(payloadPart));
        return $"{payloadPart}.{signaturePart}";
    }

    public SessionInfo? Get(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var dot = token.IndexOf('.');
        if (dot <= 0 || dot == token.Length - 1) return null;

        var payloadPart = token[..dot];
        var signaturePart = token[(dot + 1)..];

        byte[] expectedSig;
        byte[] providedSig;
        try
        {
            expectedSig = Sign(payloadPart);
            providedSig = Base64UrlDecode(signaturePart);
        }
        catch
        {
            return null;
        }

        if (expectedSig.Length != providedSig.Length || !CryptographicOperations.FixedTimeEquals(expectedSig, providedSig))
            return null;

        Payload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(Base64UrlDecode(payloadPart));
        }
        catch
        {
            return null;
        }

        if (payload is null) return null;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > payload.Exp) return null;
        if (_revoked.ContainsKey(payload.Jti)) return null;

        return new SessionInfo(payload.Username, payload.Role, payload.SquadId);
    }

    public void Remove(string token)
    {
        if (string.IsNullOrEmpty(token)) return;
        var dot = token.IndexOf('.');
        if (dot <= 0) return;

        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(Base64UrlDecode(token[..dot]));
            if (payload is not null)
            {
                _revoked[payload.Jti] = DateTimeOffset.FromUnixTimeSeconds(payload.Exp);
            }
        }
        catch
        {
            // invalid token 
        }

        CleanupRevoked();
    }

    private void CleanupRevoked()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _revoked)
        {
            if (kvp.Value < now) _revoked.TryRemove(kvp.Key, out _);
        }
    }

    private byte[] Sign(string payloadPart)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}

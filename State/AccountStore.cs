using System.Security.Cryptography;
using System.Text.Json;
using GvGPoc.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IAccountStore
{
    UserAccount? Find(string username);
    IReadOnlyList<UserAccount> All();
    void Upsert(string username, UserRole role, string? squadId, string? plainPassword);



    void AssignRoleAndSquad(string username, UserRole role, string? squadId);
    void Rename(string oldUsername, string newUsername);
    void Delete(string username);
    bool VerifyPassword(UserAccount account, string plainPassword);
    UserAccount? FindByDiscordId(string discordId);
    void LinkDiscord(string username, string discordId, string discordUsername);
    void RecordLogin(string username, string ip);

}


public class FileAccountStore : IAccountStore
{
    private readonly string _filePath;
    private readonly object _fileLock = new();
    private List<UserAccount> _accounts;

    private const int Pbkdf2Iterations = 100_000;
    private const int HashSizeBytes = 32;
    private const int SaltSizeBytes = 16;

    public FileAccountStore(IHostEnvironment env, IConfiguration config)
    {
        _filePath = Path.Combine(env.ContentRootPath, "accounts.json");
        _accounts = Load();


        if (_accounts.Count == 0)
        {
            var seedLogin = config["AdminSeedLogin"] ?? "admin";
            var seedPassword = config["AdminSeedPassword"] ?? "changeme";
            Upsert(seedLogin, UserRole.Admin, null, seedPassword);

 
            var devLogin = config["DeveloperSeedLogin"];
            var devPassword = config["DeveloperSeedPassword"];
            if (!string.IsNullOrWhiteSpace(devLogin) && !string.IsNullOrWhiteSpace(devPassword))
                Upsert(devLogin, UserRole.Developer, null, devPassword);
        }
    }

    private List<UserAccount> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new List<UserAccount>();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<UserAccount>>(json) ?? new List<UserAccount>();
        }
        catch
        {
            return new List<UserAccount>();
        }
    }

    private void Save()
    {
        lock (_fileLock)
        {
            var json = JsonSerializer.Serialize(_accounts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }

    public UserAccount? Find(string username) =>
        _accounts.FirstOrDefault(a => string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<UserAccount> All() => _accounts;

    public void Upsert(string username, UserRole role, string? squadId, string? plainPassword)
    {
        var existing = Find(username);
        string passwordHash;
        string salt;

        if (!string.IsNullOrEmpty(plainPassword))
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(plainPassword, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
            salt = Convert.ToBase64String(saltBytes);
            passwordHash = Convert.ToBase64String(hashBytes);
        }
        else if (existing is not null)
        {
            salt = existing.PasswordSalt;
            passwordHash = existing.PasswordHash;
        }
        else
        {
            throw new InvalidOperationException("Password is required when creating a new account.");
        }

        var account = new UserAccount {
            Id = existing?.Id ?? Guid.NewGuid().ToString(),
            Username = username,
            Role = role,
            SquadId = squadId,
            PasswordHash = passwordHash,
            PasswordSalt = salt,
            DiscordId = existing?.DiscordId,
            DiscordUsername = existing?.DiscordUsername,
            LastLoginAt = existing?.LastLoginAt,
            LastLoginIp = existing?.LastLoginIp
        };

        _accounts = _accounts
            .Where(a => !string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase))
            .Append(account)
            .ToList();
        Save();
    }

    public void AssignRoleAndSquad(string username, UserRole role, string? squadId)
    {
        var existing = Find(username) ?? throw new InvalidOperationException("Account not found.");

        var updated = new UserAccount {
            Id = existing.Id,
            Username = existing.Username,
            Role = role,
            SquadId = squadId,
            PasswordHash = existing.PasswordHash,
            PasswordSalt = existing.PasswordSalt,
            DiscordId = existing.DiscordId,
            DiscordUsername = existing.DiscordUsername,
            LastLoginAt = existing.LastLoginAt,
            LastLoginIp = existing.LastLoginIp
        };
        _accounts = _accounts
            .Where(a => a.Id != existing.Id)
            .Append(updated)
            .ToList();
        Save();
    }

    public void Rename(string oldUsername, string newUsername)
    {
        var existing = Find(oldUsername) ?? throw new InvalidOperationException("Account not found.");

        if (string.Equals(existing.Username, newUsername, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("New username must be different.");

        if (Find(newUsername) is not null)
            throw new InvalidOperationException("That username is already taken.");

        var updated = new UserAccount {
            Id = existing.Id,
            Username = newUsername,
            Role = existing.Role,
            SquadId = existing.SquadId,
            PasswordHash = existing.PasswordHash,
            PasswordSalt = existing.PasswordSalt,
            DiscordId = existing.DiscordId,
            DiscordUsername = existing.DiscordUsername,
            LastLoginAt = existing.LastLoginAt,
            LastLoginIp = existing.LastLoginIp
        };
        _accounts = _accounts
            .Where(a => a.Id != existing.Id)
            .Append(updated)
            .ToList();
        Save();
    }

    public void RecordLogin(string username, string ip)
    {
        var existing = Find(username);
        if (existing is null) return;

        existing.LastLoginAt = DateTimeOffset.UtcNow;
        existing.LastLoginIp = ip;
        Save();
    }

    public void Delete(string username)
    {
        _accounts = _accounts
            .Where(a => !string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Save();
    }

    public bool VerifyPassword(UserAccount account, string plainPassword)
    {
        var saltBytes = Convert.FromBase64String(account.PasswordSalt);
        var expected = Convert.FromBase64String(account.PasswordHash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(plainPassword, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public UserAccount? FindByDiscordId(string discordId) => _accounts.FirstOrDefault(a => a.DiscordId == discordId);

    public void LinkDiscord(string username, string discordId, string discordUsername)
    {
        var existing = Find(username) ?? throw new InvalidOperationException("Account not found.");

        var updated = new UserAccount {
            Id = existing.Id,
            Username = existing.Username,
            Role = existing.Role,
            SquadId = existing.SquadId,
            PasswordHash = existing.PasswordHash,
            PasswordSalt = existing.PasswordSalt,
            DiscordId = discordId,
            DiscordUsername = discordUsername,
            LastLoginAt = existing.LastLoginAt,
            LastLoginIp = existing.LastLoginIp
        };
        _accounts = _accounts
            .Where(a => a.Id != existing.Id)
            .Append(updated)
            .ToList();
        Save();
    }
}
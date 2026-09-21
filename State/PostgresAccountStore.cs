using System.Security.Cryptography;
using Dapper;
using GvGPoc.Data;
using GvGPoc.Models;
using Microsoft.Extensions.Configuration;

namespace GvGPoc.State;

public class PostgresAccountStore : IAccountStore
{
    private readonly IDbConnectionFactory _db;

    private const int Pbkdf2Iterations = 100_000;
    private const int HashSizeBytes = 32;
    private const int SaltSizeBytes = 16;


    static PostgresAccountStore()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public PostgresAccountStore(IDbConnectionFactory db, IConfiguration config)
    {
        _db = db;
        EnsureSeedAdmin(config);
    }


    private void EnsureSeedAdmin(IConfiguration config)
    {
        if (All().Count > 0) return;

        var seedLogin = config["AdminSeedLogin"] ?? "admin";
        var seedPassword = config["AdminSeedPassword"] ?? "changeme";
        Upsert(seedLogin, UserRole.Admin, null, seedPassword);

        var devLogin = config["DeveloperSeedLogin"];
        var devPassword = config["DeveloperSeedPassword"];
        if (!string.IsNullOrWhiteSpace(devLogin) && !string.IsNullOrWhiteSpace(devPassword))
            Upsert(devLogin, UserRole.Developer, null, devPassword);
    }

    private static UserAccount Map(UserRow row) => new()
    {
        Id = row.Id.ToString(),
        Username = row.Username,
        PasswordHash = row.PasswordHash,
        PasswordSalt = row.PasswordSalt,
        Role = Enum.Parse<UserRole>(row.Role),
        SquadId = row.SquadId,
        DiscordId = row.DiscordId,
        DiscordUsername = row.DiscordUsername,

        LastLoginAt = row.LastLoginAt is { } dt ? new DateTimeOffset(dt, TimeSpan.Zero) : null,
        LastLoginIp = row.LastLoginIp
    };


    private record UserRow(
        Guid Id, string Username, string PasswordHash, string PasswordSalt, string Role,
        string? SquadId, string? DiscordId, string? DiscordUsername,
        DateTime? LastLoginAt, string? LastLoginIp);

    public UserAccount? Find(string username)
    {
        using var conn = _db.Create();
        var row = conn.QuerySingleOrDefault<UserRow>(
            "SELECT * FROM users WHERE lower(username) = lower(@username)",
            new { username });
        return row is null ? null : Map(row);
    }

    public IReadOnlyList<UserAccount> All()
    {
        using var conn = _db.Create();
        var rows = conn.Query<UserRow>("SELECT * FROM users ORDER BY username");
        return rows.Select(Map).ToList();
    }

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

        using var conn = _db.Create();

        if (existing is null)
        {
            conn.Execute(
                """
                INSERT INTO users (username, password_hash, password_salt, role, squad_id)
                VALUES (@username, @passwordHash, @salt, @role, @squadId)
                """,
                new { username, passwordHash, salt, role = role.ToString(), squadId });
        }
        else
        {

            conn.Execute(
                """
                UPDATE users
                SET password_hash = @passwordHash, password_salt = @salt,
                    role = @role, squad_id = @squadId
                WHERE id = @id
                """,
                new { id = Guid.Parse(existing.Id), passwordHash, salt, role = role.ToString(), squadId });
        }
    }

    public void AssignRoleAndSquad(string username, UserRole role, string? squadId)
    {
        var existing = Find(username) ?? throw new InvalidOperationException("Account not found.");
        using var conn = _db.Create();
        conn.Execute(
            "UPDATE users SET role = @role, squad_id = @squadId WHERE id = @id",
            new { id = Guid.Parse(existing.Id), role = role.ToString(), squadId });
    }

    public void Rename(string oldUsername, string newUsername)
    {
        var existing = Find(oldUsername) ?? throw new InvalidOperationException("Account not found.");

        if (string.Equals(existing.Username, newUsername, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("New username must be different.");

        if (Find(newUsername) is not null)
            throw new InvalidOperationException("That username is already taken.");

        using var conn = _db.Create();
        conn.Execute(
            "UPDATE users SET username = @newUsername WHERE id = @id",
            new { id = Guid.Parse(existing.Id), newUsername });
    }

    public void RecordLogin(string username, string ip)
    {
        using var conn = _db.Create();
        conn.Execute(
            "UPDATE users SET last_login_at = @now, last_login_ip = @ip WHERE lower(username) = lower(@username)",
            new { now = DateTimeOffset.UtcNow, ip, username });
    }

    public void Delete(string username)
    {
        using var conn = _db.Create();
        conn.Execute("DELETE FROM users WHERE lower(username) = lower(@username)", new { username });
    }

    public bool VerifyPassword(UserAccount account, string plainPassword)
    {
        var saltBytes = Convert.FromBase64String(account.PasswordSalt);
        var expected = Convert.FromBase64String(account.PasswordHash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(plainPassword, saltBytes, Pbkdf2Iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public UserAccount? FindByDiscordId(string discordId)
    {
        using var conn = _db.Create();
        var row = conn.QuerySingleOrDefault<UserRow>(
            "SELECT * FROM users WHERE discord_id = @discordId", new { discordId });
        return row is null ? null : Map(row);
    }

    public void LinkDiscord(string username, string discordId, string discordUsername)
    {
        var existing = Find(username) ?? throw new InvalidOperationException("Account not found.");
        using var conn = _db.Create();
        conn.Execute(
            "UPDATE users SET discord_id = @discordId, discord_username = @discordUsername WHERE id = @id",
            new { id = Guid.Parse(existing.Id), discordId, discordUsername });
    }
}

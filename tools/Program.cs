using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Npgsql;
using GvGPoc.Models;

// Usage: dotnet run -- <mode> <connection-string>
// Modes: accounts, roster, order-macros, goal-order-macros, member-presets, discord-webhooks
// Example: dotnet run -- accounts accounts.json "Host=localhost;Database=tacnet;Username=postgres;Password=..."

if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run -- <mode> <connection-string>");
    Console.WriteLine("Modes: accounts, roster, order-macros, goal-order-macros, member-presets, discord-webhooks");
    return 1;
}

var mode = args[0].ToLower();
var jsonPath = $"{mode}.json";
var connectionString = args[1];

if (!File.Exists(jsonPath))
{
    Console.WriteLine($"File not found: {jsonPath}");
    return 1;
}

var json = await File.ReadAllTextAsync(jsonPath);
await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

switch (mode)
{
    case "accounts":
        await MigrateAccounts(conn, json);
        break;
    case "roster":
        await MigrateRoster(conn, json);
        break;
    case "order-macros":
        await MigrateOrderMacros(conn, json);
        break;
    case "goal-order-macros":
        await MigrateGoalOrderMacros(conn, json);
        break;
    case "member-presets":
        await MigrateMemberPresets(conn, json);
        break;
    case "discord-webhooks":
        await MigrateDiscordWebhooks(conn, json);
        break;
    default:
        Console.WriteLine($"Unknown mode: {mode}");
        return 1;
}

return 0;

async Task MigrateAccounts(NpgsqlConnection conn, string json)
{
    var accounts = JsonSerializer.Deserialize<List<JsonAccount>>(json) ?? new();
    Console.WriteLine($"Migrating {accounts.Count} accounts...");
    var inserted = 0;
    var skipped = 0;

    foreach (var a in accounts)
    {
        var rows = await conn.ExecuteAsync(
            """
            INSERT INTO users (id, username, password_hash, password_salt, role, squad_id,
                                discord_id, discord_username, last_login_at, last_login_ip)
            VALUES (@Id, @Username, @PasswordHash, @PasswordSalt, @Role, @SquadId,
                    @DiscordId, @DiscordUsername, @LastLoginAt, @LastLoginIp)
            ON CONFLICT (lower(username)) DO NOTHING
            """,
            new
            {
                Id = Guid.TryParse(a.Id, out var g) ? g : Guid.NewGuid(),
                a.Username,
                a.PasswordHash,
                a.PasswordSalt,
                Role = a.Role.ToString(),
                a.SquadId,
                a.DiscordId,
                a.DiscordUsername,
                a.LastLoginAt,
                a.LastLoginIp
            });

        if (rows > 0) inserted++; else skipped++;
    }
    Console.WriteLine($"Accounts migrated: {inserted}. Skipped: {skipped}.");
}

async Task MigrateRoster(NpgsqlConnection conn, string json)
{
    var roster = JsonSerializer.Deserialize<List<SquadRosterDto>>(json) ?? new();
    Console.WriteLine($"Migrating {roster.Count} squads...");

    await conn.ExecuteAsync("DELETE FROM squad_members");
    await conn.ExecuteAsync("DELETE FROM squads");

    var insertedSquads = 0;
    var insertedMembers = 0;

    foreach (var squad in roster)
    {
        await conn.ExecuteAsync(
            """
            INSERT INTO squads (id, label, side, leader_name, tags)
            VALUES (@Id, @Label, @Side, @LeaderName, @Tags)
            """,
            new {
                Id = squad.SquadId,
                Label = squad.Label,
                Side = squad.Side,
                LeaderName = squad.LeaderName,
                Tags = squad.Tags?.Select(t => t.ToString()).ToArray()
            });
        insertedSquads++;

        foreach (var member in squad.Members)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO squad_members (squad_id, nickname, role, bulwark, build)
                VALUES (@SquadId, @Nickname, @Role, @Bulwark, @Build)
                """,
                new {
                    SquadId = squad.SquadId,
                    member.Nickname,
                    Role = member.Role.ToString(),
                    Bulwark = member.Bulwark?.ToString(),
                    member.Build
                });
            insertedMembers++;
        }
    }
    Console.WriteLine($"Roster migrated: {insertedSquads} squads, {insertedMembers} members.");
}

async Task MigrateOrderMacros(NpgsqlConnection conn, string json)
{
    var macros = JsonSerializer.Deserialize<List<OrderMacroDto>>(json) ?? new();
    Console.WriteLine($"Migrating {macros.Count} order macros...");

    await conn.ExecuteAsync("DELETE FROM order_macro_squads");
    await conn.ExecuteAsync("DELETE FROM order_macros");

    foreach (var m in macros)
    {
        await conn.ExecuteAsync(
            """
            INSERT INTO order_macros (id, name, type)
            VALUES (@Id, @Name, @Type)
            """,
            new { Id = m.Id, Name = m.Name, Type = m.Type.ToString() });

        if (m.SquadIds != null && m.SquadIds.Count > 0)
        {
            var mappings = m.SquadIds.Select(sid => new { macro_id = m.Id, squad_id = sid });
            await conn.ExecuteAsync(
                "INSERT INTO order_macro_squads (macro_id, squad_id) VALUES (@macro_id, @squad_id)",
                mappings);
        }
    }
    Console.WriteLine("Order macros migrated.");
}

async Task MigrateGoalOrderMacros(NpgsqlConnection conn, string json)
{
    var macros = JsonSerializer.Deserialize<List<GoalOrderMacroDto>>(json) ?? new();
    Console.WriteLine($"Migrating {macros.Count} goal order macros...");

    await conn.ExecuteAsync("DELETE FROM goal_order_macro_targets");
    await conn.ExecuteAsync("DELETE FROM goal_order_macros");

    foreach (var m in macros)
    {
        await conn.ExecuteAsync(
            """
            INSERT INTO goal_order_macros (id, issuer_name, text, timer_seconds, phase)
            VALUES (@Id, @Name, @Text, @TimerSeconds, @Phase)
            """,
            new {
                Id = m.Id,
                Name = m.Name,
                Text = m.Text,
                TimerSeconds = m.TimerSeconds,
                Phase = m.Phase?.ToString()
            });

        if (m.Target != null)
        {
            var targetEntries = new List<(string Type, string Value)>();
            if (m.Target.Builds != null) targetEntries.AddRange(m.Target.Builds.Select(b => ("build", b)));
            if (m.Target.Roles != null) targetEntries.AddRange(m.Target.Roles.Select(r => ("role", r.ToString())));
            if (m.Target.Sides != null) targetEntries.AddRange(m.Target.Sides.Select(s => ("side", s)));
            if (m.Target.SquadIds != null) targetEntries.AddRange(m.Target.SquadIds.Select(sid => ("squad", sid)));

            if (targetEntries.Count > 0)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO goal_order_macro_targets (macro_id, target_type, target_value) VALUES (@macro_id, @target_type, @target_value)",
                    targetEntries.Select(te => new { macro_id = m.Id, target_type = te.Type, target_value = te.Value }));
            }
        }
    }
    Console.WriteLine("Goal order macros migrated.");
}

async Task MigrateMemberPresets(NpgsqlConnection conn, string json)
{
    var presets = JsonSerializer.Deserialize<List<MemberPresetDto>>(json) ?? new();
    Console.WriteLine($"Migrating {presets.Count} member presets...");

    await conn.ExecuteAsync("DELETE FROM member_presets");

    foreach (var p in presets)
    {
        await conn.ExecuteAsync(
            """
            INSERT INTO member_presets (nickname, role, bulwark, build)
            VALUES (@Nickname, @Role, @Bulwark, @Build)
            """,
            new {
                p.Nickname,
                Role = p.Role.ToString(),
                Bulwark = p.Bulwark?.ToString(),
                p.Build
            });
    }
    Console.WriteLine("Member presets migrated.");
}

async Task MigrateDiscordWebhooks(NpgsqlConnection conn, string json)
{
    var webhooks = JsonSerializer.Deserialize<List<DiscordChannelDto>>(json) ?? new();
    Console.WriteLine($"Migrating {webhooks.Count} discord webhooks...");

    await conn.ExecuteAsync("DELETE FROM discord_webhooks");

    foreach (var w in webhooks)
    {
        await conn.ExecuteAsync(
            """
            INSERT INTO discord_webhooks (id, name, webhook_url)
            VALUES (@Id, @Name, @WebhookUrl)
            """,
            new { Id = w.Id, Name = w.Name, WebhookUrl = w.WebhookUrl });
    }
    Console.WriteLine("Discord webhooks migrated.");
}

// DTOs for migration
record JsonAccount(
    string Id, string Username, string PasswordHash, string PasswordSalt, RoleEnum Role,
    string? SquadId, string? DiscordId, string? DiscordUsername,
    DateTimeOffset? LastLoginAt, string? LastLoginIp);

enum RoleEnum { Admin, Commander, Leader, Player, Developer }

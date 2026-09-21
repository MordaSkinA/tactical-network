using System.Text.Json;
using Dapper;
using Npgsql;

// Одноразовая утилита: читает существующий accounts.json (тот, что сейчас использует
// FileAccountStore) и переносит записи в PostgreSQL "как есть" — password_hash и
// password_salt копируются напрямую, пароли НЕ пересчитываются (у нас нет исходных
// plain-text паролей, и это правильно — мы их никогда не храним).
//
// Запуск:
//   dotnet run -- <путь-к-accounts.json> "<connection string>"
// Пример:
//   dotnet run -- ../../Local/accounts.json "Host=localhost;Database=tacnet;Username=postgres;Password=..."

if (args.Length < 2)
{
    Console.WriteLine("Использование: dotnet run -- <путь-к-accounts.json> <connection-string>");
    return 1;
}

var jsonPath = args[0];
var connectionString = args[1];

if (!File.Exists(jsonPath))
{
    Console.WriteLine($"Файл не найден: {jsonPath}");
    return 1;
}

var json = await File.ReadAllTextAsync(jsonPath);
var accounts = JsonSerializer.Deserialize<List<JsonAccount>>(json) ?? new();

Console.WriteLine($"Найдено {accounts.Count} аккаунт(ов) в {jsonPath}.");

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

var inserted = 0;
var skipped = 0;

foreach (var a in accounts)
{
    // ON CONFLICT по username (case-insensitive уникальный индекс уже создан миграцией 001) —
    // повторный запуск скрипта безопасен и не задублирует записи.
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

Console.WriteLine($"Перенесено: {inserted}. Пропущено (уже существовали): {skipped}.");
return 0;

// Отдельный DTO для чтения JSON — не завязываемся на класс UserAccount из основного
// проекта, чтобы этот инструмент оставался независимым однофайловым скриптом.
record JsonAccount(
    string Id, string Username, string PasswordHash, string PasswordSalt, RoleEnum Role,
    string? SquadId, string? DiscordId, string? DiscordUsername,
    DateTimeOffset? LastLoginAt, string? LastLoginIp);

enum RoleEnum { Admin, Commander, Leader, Player, Developer }

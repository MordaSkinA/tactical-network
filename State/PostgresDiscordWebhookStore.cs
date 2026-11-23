using Dapper;
using GvGPoc.Data;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public class PostgresDiscordWebhookStore : IDiscordWebhookStore
{
    private readonly IDbConnectionFactory _db;

    static PostgresDiscordWebhookStore()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public PostgresDiscordWebhookStore(IDbConnectionFactory db)
    {
        _db = db;
    }

    private record WebhookRow(string Id, string Name, string WebhookUrl);

    private static DiscordChannelDto Map(WebhookRow row) => new(row.Id, row.Name, row.WebhookUrl);

    public IReadOnlyList<DiscordChannelDto> All()
    {
        using var conn = _db.Create();
        var rows = conn.Query<WebhookRow>("SELECT id, name, webhook_url FROM discord_webhooks");
        return rows.Select(Map).ToList();
    }

    public DiscordChannelDto? Find(string id)
    {
        using var conn = _db.Create();
        var row = conn.QuerySingleOrDefault<WebhookRow>(
            "SELECT id, name, webhook_url FROM discord_webhooks WHERE id = @id",
            new { id });
        return row is null ? null : Map(row);
    }

    public async Task<DiscordChannelDto> UpsertAsync(string? id, string name, string webhookUrl)
    {
        var targetId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO discord_webhooks (id, name, webhook_url)
            VALUES (@Id, @Name, @WebhookUrl)
            ON CONFLICT (id) DO UPDATE SET
                name = EXCLUDED.name,
                webhook_url = EXCLUDED.webhook_url
            """,
            new { Id = targetId, Name = name, WebhookUrl = webhookUrl });

        return new DiscordChannelDto(targetId, name, webhookUrl);
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("DELETE FROM discord_webhooks WHERE id = @id", new { id });
    }
}

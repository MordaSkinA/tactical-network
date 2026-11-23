using Dapper;
using GvGPoc.Data;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public class PostgresOrderMacroStore : IOrderMacroStore
{
    private readonly IDbConnectionFactory _db;

    static PostgresOrderMacroStore()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public PostgresOrderMacroStore(IDbConnectionFactory db)
    {
        _db = db;
    }

    public IReadOnlyList<OrderMacroDto> All()
    {
        using var conn = _db.Create();
        return conn.Query<OrderMacroDto>("SELECT id, name, type FROM order_macros").ToList();
    }

    public OrderMacroDto? Find(string id)
    {
        using var conn = _db.Create();
        return conn.QuerySingleOrDefault<OrderMacroDto>("SELECT id, name, type FROM order_macros WHERE id = @id", new { id });
    }

    public async Task<OrderMacroDto> UpsertAsync(string? id, string name, OrderType type, List<string>? squadIds)
    {
        var targetId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
        using var conn = _db.Create();

        await conn.ExecuteAsync(
            """
            INSERT INTO order_macros (id, name, type)
            VALUES (@Id, @Name, @Type)
            ON CONFLICT (id) DO UPDATE SET
                name = EXCLUDED.name,
                type = EXCLUDED.type
            """,
            new { Id = targetId, Name = name, Type = type.ToString() });

        await conn.ExecuteAsync("DELETE FROM order_macro_squads WHERE macro_id = @id", new { id = targetId });

        if (squadIds != null && squadIds.Count > 0)
        {
            var mappings = squadIds.Select(sid => new { macro_id = targetId, squad_id = sid });
            await conn.ExecuteAsync(
                "INSERT INTO order_macro_squads (macro_id, squad_id) VALUES (@macro_id, @squad_id)",
                mappings);
        }

        return new OrderMacroDto(targetId, name, type, squadIds ?? new());
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("DELETE FROM order_macros WHERE id = @id", new { id });
    }
}

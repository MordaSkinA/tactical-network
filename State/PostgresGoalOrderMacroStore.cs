using Dapper;
using GvGPoc.Data;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;
using System.Data;

namespace GvGPoc.State;

public class PostgresGoalOrderMacroStore : IGoalOrderMacroStore
{
    private readonly IDbConnectionFactory _db;

    static PostgresGoalOrderMacroStore()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public PostgresGoalOrderMacroStore(IDbConnectionFactory db)
    {
        _db = db;
    }

    private record MacroRow(string Id, string IssuerName, string Text, int? TimerSeconds, string? Phase);

    public IReadOnlyList<GoalOrderMacroDto> All()
    {
        using var conn = _db.Create();
        var rows = conn.Query<MacroRow>("SELECT id, issuer_name, text, timer_seconds, phase FROM goal_order_macros").ToList();

        return rows.Select(r => {
            var id = r.Id;
            var target = GetTargetForMacro(id, conn);
            return new GoalOrderMacroDto(
                id,
                r.IssuerName,
                r.Text,
                target,
                r.TimerSeconds,
                r.Phase is not null ? Enum.Parse<BattlePhase>(r.Phase) : null
            );
        }).ToList();
    }

    public GoalOrderMacroDto? Find(string id)
    {
        using var conn = _db.Create();
        var r = conn.QuerySingleOrDefault<MacroRow>("SELECT id, issuer_name, text, timer_seconds, phase FROM goal_order_macros WHERE id = @id", new { id });
        if (r == null) return null;

        var target = GetTargetForMacro(id, conn);
        return new GoalOrderMacroDto(
            r.Id,
            r.IssuerName,
            r.Text,
            target,
            r.TimerSeconds,
            r.Phase is not null ? Enum.Parse<BattlePhase>(r.Phase) : null
        );
    }

    private GoalOrderTargetDto? GetTargetForMacro(string macroId, IDbConnection conn)
    {
        var entries = conn.Query<dynamic>(
            "SELECT target_type, target_value FROM goal_order_macro_targets WHERE macro_id = @macro_id",
            new { macro_id = macroId }).ToList();

        if (entries.Count == 0) return null;

        var builds = new List<string>();
        var roles = new List<MemberRole>();
        var sides = new List<string>();
        var squadIds = new List<string>();

        foreach (var e in entries)
        {
            string type = e.target_type;
            string val = e.target_value;

            switch (type)
            {
                case "build": builds.Add(val); break;
                case "role": roles.Add(Enum.Parse<MemberRole>(val)); break;
                case "side": sides.Add(val); break;
                case "squad": squadIds.Add(val); break;
            }
        }

        return new GoalOrderTargetDto(
            builds.Count > 0 ? builds : null,
            roles.Count > 0 ? roles : null,
            sides.Count > 0 ? sides : null,
            squadIds.Count > 0 ? squadIds : null
        );
    }

    public async Task<GoalOrderMacroDto> UpsertAsync(string? id, string name, string text, GoalOrderTargetDto? target, int? timerSeconds, BattlePhase? phase)
    {
        var targetId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
        using var conn = _db.Create();

        await conn.ExecuteAsync(
            """
            INSERT INTO goal_order_macros (id, issuer_name, text, timer_seconds, phase)
            VALUES (@Id, @Name, @Text, @TimerSeconds, @Phase)
            ON CONFLICT (id) DO UPDATE SET
                issuer_name = EXCLUDED.issuer_name,
                text = EXCLUDED.text,
                timer_seconds = EXCLUDED.timer_seconds,
                phase = EXCLUDED.phase
            """,
            new {
                Id = targetId,
                Name = name,
                Text = text,
                TimerSeconds = timerSeconds,
                Phase = phase?.ToString()
            });

        await conn.ExecuteAsync("DELETE FROM goal_order_macro_targets WHERE macro_id = @id", new { id = targetId });

        if (target != null)
        {
            var targetEntries = new List<(string Type, string Value)>();
            if (target.Builds != null) targetEntries.AddRange(target.Builds.Select(b => ("build", b)));
            if (target.Roles != null) targetEntries.AddRange(target.Roles.Select(r => ("role", r.ToString())));
            if (target.Sides != null) targetEntries.AddRange(target.Sides.Select(s => ("side", s)));
            if (target.SquadIds != null) targetEntries.AddRange(target.SquadIds.Select(sid => ("squad", sid)));

            if (targetEntries.Count > 0)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO goal_order_macro_targets (macro_id, target_type, target_value) VALUES (@macro_id, @target_type, @target_value)",
                    targetEntries.Select(te => new { macro_id = targetId, target_type = te.Type, target_value = te.Value }));
            }
        }

        return new GoalOrderMacroDto(targetId, name, text, target, timerSeconds, phase);
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync("DELETE FROM goal_order_macros WHERE id = @id", new { id });
    }
}

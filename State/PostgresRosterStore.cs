using Dapper;
using GvGPoc.Data;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public class PostgresRosterStore : IRosterStore
{
    private readonly IDbConnectionFactory _db;

    static PostgresRosterStore()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public PostgresRosterStore(IDbConnectionFactory db)
    {
        _db = db;
    }

    public IReadOnlyList<SquadRosterDto> GetRoster()
    {
        using var conn = _db.Create();

        var squads = conn.Query<dynamic>(
            "SELECT id, label, side, leader_name, tags FROM squads"
        ).ToList();

        var members = conn.Query<dynamic>(
            "SELECT squad_id, nickname, role, bulwark, build FROM squad_members"
        ).ToList();

        var roster = squads.Select(s => new SquadRosterDto(
            SquadId: s.id,
            Side: s.side ?? "Flex",
            LeaderName: s.leader_name,
            Members: members
                .Where(m => m.squad_id == s.id)
                .Select(m => new SquadMemberDto(
                    m.nickname,
                    Enum.Parse<MemberRole>(m.role),
                    m.bulwark != null ? Enum.Parse<BulwarkPosition>(m.bulwark) : null,
                    m.build
                )).ToList(),
            Label: s.label,
            Tags: s.tags != null ? ((string[])s.tags).Select(t => Enum.Parse<MemberTag>(t)).ToList() : null
        )).ToList();

        return roster;
    }

    public async Task SetRosterAsync(IReadOnlyList<SquadRosterDto> roster)
    {
        using var conn = _db.Create();
        using var trans = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync("DELETE FROM squad_members", transaction: trans);
            await conn.ExecuteAsync("DELETE FROM squads", transaction: trans);

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
                    },
                    transaction: trans);

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
                        },
                        transaction: trans);
                }
            }

            trans.Commit();
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }
}

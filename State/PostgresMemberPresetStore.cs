using Dapper;
using GvGPoc.Data;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public class PostgresMemberPresetStore : IMemberPresetStore
{
    private readonly IDbConnectionFactory _db;

    static PostgresMemberPresetStore()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public PostgresMemberPresetStore(IDbConnectionFactory db)
    {
        _db = db;
    }

    private record PresetRow(string Nickname, string Role, string? Bulwark, string? Build);

    public IReadOnlyDictionary<string, MemberPresetDto> All()
    {
        using var conn = _db.Create();
        var rows = conn.Query<PresetRow>("SELECT nickname, role, bulwark, build FROM member_presets");
        return rows.ToDictionary(
            r => r.Nickname,
            r => new MemberPresetDto(
                r.Nickname,
                Enum.Parse<MemberRole>(r.Role),
                r.Bulwark != null ? Enum.Parse<BulwarkPosition>(r.Bulwark) : null,
                r.Build
            ),
            StringComparer.OrdinalIgnoreCase
        );
    }

    public MemberPresetDto? Find(string nickname)
    {
        using var conn = _db.Create();
        var row = conn.QuerySingleOrDefault<PresetRow>(
            "SELECT nickname, role, bulwark, build FROM member_presets WHERE nickname = @nickname",
            new { nickname });
        return row is null ? null : new MemberPresetDto(
            row.Nickname,
            Enum.Parse<MemberRole>(row.Role),
            row.Bulwark != null ? Enum.Parse<BulwarkPosition>(row.Bulwark) : null,
            row.Build
        );
    }

    public async Task SaveAsync(SquadMemberDto member)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO member_presets (nickname, role, bulwark, build)
            VALUES (@Nickname, @Role, @Bulwark, @Build)
            ON CONFLICT (nickname) DO UPDATE SET
                role = EXCLUDED.role,
                bulwark = EXCLUDED.bulwark,
                build = EXCLUDED.build
            """,
            new {
                member.Nickname,
                Role = member.Role.ToString(),
                Bulwark = member.Bulwark?.ToString(),
                member.Build
            });
    }

    public async Task SaveManyAsync(IEnumerable<SquadMemberDto> members)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            """
            INSERT INTO member_presets (nickname, role, bulwark, build)
            VALUES (@Nickname, @Role, @Bulwark, @Build)
            ON CONFLICT (nickname) DO UPDATE SET
                role = EXCLUDED.role,
                bulwark = EXCLUDED.bulwark,
                build = EXCLUDED.build
            """,
            members.Select(m => new {
                m.Nickname,
                Role = m.Role.ToString(),
                Bulwark = m.Bulwark?.ToString(),
                m.Build
            }));
    }
}

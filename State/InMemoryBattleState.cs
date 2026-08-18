using System.Collections.Concurrent;
using System.Text.Json;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IBattleState
{
    BattleEventPushDto AddEvent(ReportEventDto dto);
    OrderPushDto AddOrder(IssueOrderDto dto);
    BattleEventPushDto AddSos(SosDto dto);

    // История и ростер
    IReadOnlyList<object> GetRecentHistory();
    IReadOnlyList<SquadRosterDto> GetRoster();
    void SetRoster(IReadOnlyList<SquadRosterDto> roster);
    void ResetHistory();

    // Пользователи и Авторизация
    UserAccount? ValidateUser(string username, string password);
    bool CreateUser(UserAccount newUser);
    IReadOnlyList<UserAccount> GetUsers();
}


public class InMemoryBattleState : IBattleState
{
    private readonly ConcurrentQueue<object> _history = new();
    private const int MaxHistory = 200;
    private readonly string _rosterFilePath;
    private readonly object _rosterFileLock = new();
    private readonly ConcurrentDictionary<string, UserAccount> _users = new(StringComparer.OrdinalIgnoreCase);


    private IReadOnlyList<SquadRosterDto> _roster;


    public InMemoryBattleState(IHostEnvironment env)
    {
        _rosterFilePath = Path.Combine(env.ContentRootPath, "roster.json");
        _roster = LoadRosterFromDisk();
        //аккаунты для тестирования
        CreateUser(new UserAccount { Username = "admin", PasswordHash = "admin123", Role = UserRole.Admin });
        CreateUser(new UserAccount { Username = "commander", PasswordHash = "cmd123", Role = UserRole.Commander });
        CreateUser(new UserAccount { Username = "leader_d1", PasswordHash = "lead123", Role = UserRole.Leader, SquadId = "D1" });
    }

    private IReadOnlyList<SquadRosterDto> LoadRosterFromDisk()
    {
        try
        {
            if (!File.Exists(_rosterFilePath)) return Array.Empty<SquadRosterDto>();
            var json = File.ReadAllText(_rosterFilePath);
            var roster = JsonSerializer.Deserialize<List<SquadRosterDto>>(json);
            return roster ?? new List<SquadRosterDto>();
        }
        catch
        {

            return Array.Empty<SquadRosterDto>();
        }
    }

    

    public BattleEventPushDto AddEvent(ReportEventDto dto)
    {
        var evt = new BattleEventPushDto(
            EventId: Guid.NewGuid(),
            ReporterName: dto.ReporterName,
            EnemyRole: dto.EnemyRole,
            TargetSquadId: dto.TargetSquadId,
            Note: dto.Note,
            Severity: EscalateSeverity(dto),
            Timestamp: DateTimeOffset.UtcNow
        );

        Track(evt);
        return evt;
    }

    public OrderPushDto AddOrder(IssueOrderDto dto)
    {
        var order = new OrderPushDto(
            OrderId: Guid.NewGuid(),
            IssuerName: dto.IssuerName,
            Type: dto.Type,
            TargetSquadId: dto.TargetSquadId,
            IssuedAt: DateTimeOffset.UtcNow
        );

        Track(order);
        return order;
    }

    public BattleEventPushDto AddSos(SosDto dto)
    {
        // переделать потом !!!

        var evt = new BattleEventPushDto(
            EventId: Guid.NewGuid(),
            ReporterName: dto.ReporterName,
            EnemyRole: null,
            TargetSquadId: dto.SquadId,
            Note: "SOS",
            Severity: EventSeverity.Critical,
            Timestamp: DateTimeOffset.UtcNow
        );

        Track(evt);
        return evt;
    }

    public IReadOnlyList<object> GetRecentHistory() => _history.ToArray();

    public IReadOnlyList<SquadRosterDto> GetRoster() => _roster;

    public void SetRoster(IReadOnlyList<SquadRosterDto> roster)
    {
        _roster = roster;

        lock (_rosterFileLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(roster, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_rosterFilePath, json);
            }
            catch
            {

            }
        }
    }

    public void ResetHistory()
    {
        while (_history.TryDequeue(out _))
        {
        }
    }

    private void Track(object item)
    {
        _history.Enqueue(item);
        while (_history.Count > MaxHistory && _history.TryDequeue(out _))
        {
        }
    }

    // Зхаглушка 

    private static EventSeverity EscalateSeverity(ReportEventDto dto) =>
        dto.EnemyRole switch
        {
            EnemyRole.TwinBlades => EventSeverity.Critical,
            EnemyRole.Healer => EventSeverity.Warning,
            _ => EventSeverity.Info
        };

    public UserAccount? ValidateUser(string username, string password)
    {
        if (_users.TryGetValue(username, out var user))
        {
            if (user.PasswordHash == password)
            {
                return user;
            }
        }
        return null;
    }

    public bool CreateUser(UserAccount newUser)
    {
        return _users.TryAdd(newUser.Username, newUser);
    }

    public IReadOnlyList<UserAccount> GetUsers()
    {
        return _users.Values.ToList();
    }

}

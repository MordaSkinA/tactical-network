using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IBattleState
{
    BattleEventPushDto AddEvent(ReportEventDto dto, string reporterName, string targetSquadId);
    OrderPushDto AddOrder(IssueOrderDto dto, string issuerName, string targetSquadId);
    BattleEventPushDto AddSos(string reporterName, string squadId);

    IReadOnlyList<object> GetRecentHistory();

    IReadOnlyList<SquadRosterDto> GetRoster();
    void SetRoster(IReadOnlyList<SquadRosterDto> roster);

    string SaveLogSnapshot();
    void ResetHistory();
}

public class InMemoryBattleState : IBattleState
{
    private readonly ConcurrentQueue<object> _history = new();
    private const int MaxHistory = 200;

    private readonly string _rosterFilePath;
    private readonly string _logsDirectory;
    private readonly object _rosterFileLock = new();
    private readonly object _logFileLock = new();

    private IReadOnlyList<SquadRosterDto> _roster;

    public InMemoryBattleState(IHostEnvironment env)
    {
        _rosterFilePath = Path.Combine(env.ContentRootPath, "roster.json");
        _logsDirectory = Path.Combine(env.ContentRootPath, "logs");
        Directory.CreateDirectory(_logsDirectory);
        _roster = LoadRosterFromDisk();
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

    public BattleEventPushDto AddEvent(ReportEventDto dto, string reporterName, string targetSquadId)
    {
        var evt = new BattleEventPushDto(
            EventId: Guid.NewGuid(),
            ReporterName: reporterName,
            EnemyRole: dto.EnemyRole,
            TargetSquadId: targetSquadId,
            Note: dto.Note,
            Severity: EscalateSeverity(dto),
            Timestamp: DateTimeOffset.UtcNow
        );
        Track(evt);
        return evt;
    }

    public OrderPushDto AddOrder(IssueOrderDto dto, string issuerName, string targetSquadId)
    {
        var order = new OrderPushDto(
            OrderId: Guid.NewGuid(),
            IssuerName: issuerName,
            Type: dto.Type,
            TargetSquadId: targetSquadId,
            IssuedAt: DateTimeOffset.UtcNow
        );
        Track(order);
        return order;
    }

    public BattleEventPushDto AddSos(string reporterName, string squadId)
    {
        var evt = new BattleEventPushDto(
            EventId: Guid.NewGuid(),
            ReporterName: reporterName,
            EnemyRole: null,
            TargetSquadId: squadId,
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

    public string SaveLogSnapshot()
    {
        var baseName = $"battle-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
        var jsonPath = Path.Combine(_logsDirectory, baseName + ".json");
        var txtPath = Path.Combine(_logsDirectory, baseName + ".txt");
        lock (_logFileLock)
        {
            var items = _history.ToArray();
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonPath, json);
            File.WriteAllText(txtPath, FormatReadableLog(items));
        }
        return baseName + ".txt";
    }

    private static string FormatReadableLog(IReadOnlyList<object> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            if (item is BattleEventPushDto evt)
            {
                var kind = evt.Note == "SOS" ? "SOS  " : "EVENT";
                var detail = evt.EnemyRole is not null ? evt.EnemyRole.ToString() : (evt.Note ?? "-");
                sb.AppendLine(
                    $"{evt.Timestamp:yyyy-MM-dd HH:mm:ss} UTC | {kind} | squad {evt.TargetSquadId,-4} | {evt.Severity,-8} | {detail} — reported by {evt.ReporterName}");
            }
            else if (item is OrderPushDto order)
            {
                sb.AppendLine(
                    $"{order.IssuedAt:yyyy-MM-dd HH:mm:ss} UTC | ORDER | squad {order.TargetSquadId,-4} | {"",-8} | {order.Type} — issued by {order.IssuerName}");
            }
        }
        return sb.ToString();
    }

    public void ResetHistory()
    {
        if (!_history.IsEmpty) SaveLogSnapshot();
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

    private static EventSeverity EscalateSeverity(ReportEventDto dto) =>
        dto.EnemyRole switch {
            EnemyRole.TwinBlades => EventSeverity.Critical,
            EnemyRole.Tank => EventSeverity.Critical,
            EnemyRole.Nameless => EventSeverity.Warning,
            EnemyRole.Healer => EventSeverity.Warning,
            _ => EventSeverity.Info
        };
}
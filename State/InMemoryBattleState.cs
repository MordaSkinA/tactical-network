using GvGPoc.Models;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GvGPoc.State;

public interface IBattleState
{
    BattleEventPushDto AddEvent(ReportEventDto dto, string reporterName, string targetSquadId);
    OrderPushDto AddOrder(IssueOrderDto dto, string issuerName, string targetSquadId);
    BattleEventPushDto AddSos(string reporterName, string squadId);

    IReadOnlyList<object> GetRecentHistory();

    IReadOnlyList<SquadRosterDto> GetRoster();
    void SetRoster(IReadOnlyList<SquadRosterDto> roster);
    IReadOnlyList<LogFileSummaryDto> ListLogFiles();
    string? ReadLogFile(string fileName);

    string SaveLogSnapshot();

    SquadStatusPushDto AddSquadStatus(ReportSquadStatusDto dto, string reporterName, string squadId);

    BattleStatusDto GetBattleStatus();
    BattleStatusDto StartBattle();
    void EndBattle();
}

public class InMemoryBattleState : IBattleState
{
    private readonly ConcurrentQueue<object> _history = new();
    private const int MaxHistory = 200;

    private readonly string _rosterFilePath;
    private readonly string _logsDirectory;
    private readonly object _rosterFileLock = new();
    private readonly object _logFileLock = new();
    private static readonly JsonSerializerOptions ReadOptions = new() {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions WriteOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private IReadOnlyList<SquadRosterDto> _roster;

    private bool _battleActive;
    private DateTimeOffset? _battleStartedAt;

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

    public SquadStatusPushDto AddSquadStatus(ReportSquadStatusDto dto, string reporterName, string squadId)
    {
        var status = new SquadStatusPushDto(
            StatusId: Guid.NewGuid(),
            ReporterName: reporterName,
            Type: dto.Type,
            TargetSquadId: squadId,
            Timestamp: DateTimeOffset.UtcNow
        );
        Track(status);
        return status;
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
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            });
            File.WriteAllText(jsonPath, json);
            File.WriteAllText(txtPath, FormatReadableLog(items));
        }
        return baseName + ".txt";
    }

    public IReadOnlyList<LogFileSummaryDto> ListLogFiles()
    {
        return Directory.EnumerateFiles(_logsDirectory, "battle-*.json")
            .Select(p => new FileInfo(p))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(f => new LogFileSummaryDto(f.Name, f.LastWriteTimeUtc))
            .ToList();
    }

    public string? ReadLogFile(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(safeName) || !safeName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return null;

        var path = Path.Combine(_logsDirectory, safeName);
        if (!File.Exists(path)) return null;


        var raw = File.ReadAllText(path).Replace("\"StandingBy\"", "\"Autonomous\"");

        using var doc = JsonDocument.Parse(raw);
        var items = new List<object>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            object item;
            if (el.TryGetProperty("EventId", out _) || el.TryGetProperty("eventId", out _))
                item = el.Deserialize<BattleEventPushDto>(ReadOptions)!;
            else if (el.TryGetProperty("OrderId", out _) || el.TryGetProperty("orderId", out _))
                item = el.Deserialize<OrderPushDto>(ReadOptions)!;
            else
                item = el.Deserialize<SquadStatusPushDto>(ReadOptions)!;
            items.Add(item);
        }
        return JsonSerializer.Serialize(items, WriteOptions);
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
            else if (item is SquadStatusPushDto status)
            {
                sb.AppendLine(
                    $"{status.Timestamp:yyyy-MM-dd HH:mm:ss} UTC | STATUS | squad {status.TargetSquadId,-4} | {"",-8} | {status.Type} — reported by {status.ReporterName}");
            }
        }
        return sb.ToString();
    }


    public BattleStatusDto GetBattleStatus() => new(_battleActive, _battleStartedAt);

    public BattleStatusDto StartBattle()
    {
        _battleActive = true;
        _battleStartedAt = DateTimeOffset.UtcNow;
        return GetBattleStatus();
    }

    public void EndBattle()
    {
        if (!_history.IsEmpty) SaveLogSnapshot();
        while (_history.TryDequeue(out _))
        {
        }
        _battleActive = false;
        _battleStartedAt = null;
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
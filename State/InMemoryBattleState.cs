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

    // Возвращает недавнюю историю для клиентов, которые только что подключились
    // (аналог BattleSnapshotDto из архитектурного документа, упрощённый).
    IReadOnlyList<object> GetRecentHistory();

    IReadOnlyList<SquadRosterDto> GetRoster();
    void SetRoster(IReadOnlyList<SquadRosterDto> roster);

    void ResetHistory();
}

// Singleton на всё время жизни процесса. При перезапуске сервера история теряется —
// это осознанное ограничение POC, в реальной системе это заменяется на BattleEvents
// в PostgreSQL + BattleStateProjector (см. архитектурный документ, разделы 3 и 9).
public class InMemoryBattleState : IBattleState
{
    private readonly ConcurrentQueue<object> _history = new();
    private const int MaxHistory = 200;

    private readonly string _rosterFilePath;
    private readonly object _rosterFileLock = new();

    // Присвоение ссылки на список атомарно в .NET, для POC-масштаба этого достаточно
    // вместо полноценной блокировки/конкурентной коллекции на чтение.
    private IReadOnlyList<SquadRosterDto> _roster;

    // Событийная история POC-специфично остаётся только в памяти (см. README) —
    // персист попросили именно для ростера, он не меняется каждую секунду в отличие
    // от событий, поэтому простой JSON-файл на диске — адекватное решение без БД.
    public InMemoryBattleState(IHostEnvironment env)
    {
        _rosterFilePath = Path.Combine(env.ContentRootPath, "roster.json");
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
            // POC: битый/несовместимый файл не должен ронять запуск сервера —
            // просто стартуем с пустым ростером, админ пересоздаст через admin.html.
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
        // SOS переиспользует BattleEventPushDto: EnemyRole = null, Note = "SOS" —
        // клиент отличает по Note. Для настоящей системы это стал бы отдельный
        // BattleEventType.Sos в enum'е (см. архитектурный документ, раздел 4).
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
                // POC: если запись на диск не удалась (нет прав, диск занят и т.п.) —
                // не роняем hub-вызов. Ростер применится в памяти на время работы
                // процесса, но не переживёт рестарт — это лучше, чем упавший сервер
                // посреди GvG.
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

    // Заглушка под EventRoutingService из архитектурного документа (раздел 13):
    // в реальной системе здесь будут доменные правила эскалации (например,
    // "TB рядом с healer squad = всегда Critical"). Для POC — простое правило,
    // чтобы было видно на дашборде разницу в цвете между Warning и Critical.
    private static EventSeverity EscalateSeverity(ReportEventDto dto) =>
        dto.EnemyRole switch
        {
            EnemyRole.TwinBlades => EventSeverity.Critical,
            EnemyRole.Healer => EventSeverity.Warning,
            _ => EventSeverity.Info
        };
}

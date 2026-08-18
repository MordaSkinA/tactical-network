using System.Collections.Concurrent;
using GvGPoc.Models;

namespace GvGPoc.State;

public interface IBattleState
{
    BattleEventPushDto AddEvent(ReportEventDto dto);
    OrderPushDto AddOrder(IssueOrderDto dto);


    IReadOnlyList<object> GetRecentHistory();
}


public class InMemoryBattleState : IBattleState
{
    private readonly ConcurrentQueue<object> _history = new();
    private const int MaxHistory = 200;

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

    public IReadOnlyList<object> GetRecentHistory() => _history.ToArray();

    private void Track(object item)
    {
        _history.Enqueue(item);
        while (_history.Count > MaxHistory && _history.TryDequeue(out _))
        {
        }
    }

 

    private static EventSeverity EscalateSeverity(ReportEventDto dto) =>
        dto.EnemyRole switch
        {
            EnemyRole.TwinBlades => EventSeverity.Critical,
            EnemyRole.Healer => EventSeverity.Warning,
            _ => EventSeverity.Info
        };
}

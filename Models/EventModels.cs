namespace GvGPoc.Models;

public enum EnemyRole
{
    TwinBlades,
    Healer,
    Bruiser,
    Ranged,
    Group
}

public enum EventSeverity
{
    Info,
    Warning,
    Critical
}

public enum OrderType
{
    Push,
    Hold,
    FallBack,
    Rotate,
    Defend,
    ProtectHealer,
    TargetHealer,
    TargetTb
}

// Килент сервак

public record ReportEventDto(
    string ReporterName,
    EnemyRole? EnemyRole,
    string TargetSquadId,
    string? Note
);

public record IssueOrderDto(
    string IssuerName,
    OrderType Type,
    string TargetSquadId
);

// сервак клиент

public record BattleEventPushDto(
    Guid EventId,
    string ReporterName,
    EnemyRole? EnemyRole,
    string TargetSquadId,
    string? Note,
    EventSeverity Severity,
    DateTimeOffset Timestamp
);

public record OrderPushDto(
    Guid OrderId,
    string IssuerName,
    OrderType Type,
    string TargetSquadId,
    DateTimeOffset IssuedAt
);

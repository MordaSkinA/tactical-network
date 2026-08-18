namespace GvGPoc.Models;

public enum EnemyRole
{
    TwinBlades,
    Healer,
    Tank,
    Nameless,
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

public record SosDto(
    string ReporterName,
    string SquadId
);



public record SquadRosterDto(
    string SquadId,
    string Side,
    List<string> Members
);

public record UpdateRosterDto(
    string AdminKey,
    List<SquadRosterDto> Squads
);

public record AdminActionDto(
    string AdminKey
);



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

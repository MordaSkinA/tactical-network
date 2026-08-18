namespace GvGPoc.Models;

// POC-версия enum'ов из архитектурного документа (раздел 4, 12).
// В реальной системе это будет привязано к конкретному Battle/Squad из БД —
// здесь squad-id захардкожен строкой ("D1".."D3", "A1".."A3"), чтобы не тянуть БД.

public enum UserRole
{
    Admin,
    Commander,
    Leader,
    Player
}

public enum EnemyRole
{
    TwinBlades,
    Healer,
    Tank,
    Nameless,
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

public class UserAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? SquadId { get; set; }
}

public record LoginDto(
    string Username,
    string Password
);

public record AuthResponseDto(
    bool Success,
    string? Message,
    string? Username,
    UserRole? Role,
    string? SquadId
);

public record CreateUserDto(
    string AdminKey,
    string Username,
    string Password,
    UserRole Role,
    string? SquadId
);

// --- Client -> Server ---

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

// --- Roster / management (упрощённый вариант вместо полноценного Phase 1 CRUD) ---

public record SquadRosterDto(
    string SquadId,
    string Side,       // "Attack" | "Defense"
    string? LeaderName, // Имя назначенного Лидера отряда
    List<string> Members
);

public record UpdateRosterDto(
    string AdminKey,
    List<SquadRosterDto> Squads
);

public record AdminActionDto(
    string AdminKey
);

// --- Server -> Client ---

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



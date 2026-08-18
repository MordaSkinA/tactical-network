namespace GvGPoc.Models;

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

// ОТ клиента

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
    string? LeaderName, 
    List<string> Members
);

public record UpdateRosterDto(
    string AdminKey,
    List<SquadRosterDto> Squads
);

public record AdminActionDto(
    string AdminKey
);

// ОТ сервера

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



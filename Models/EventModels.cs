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
    Nameless
}

public enum EventSeverity
{
    Info,
    Warning,
    Critical
}

public enum OrderType
{
    PushBot,
    PushMid,
    PushTop,
    AttackGoose,
    Hold,
    FallBack,
    Rotate,
    DefendBot,
    DefendMid,
    DefendTop,
    DefendGoose,
    DefendTree,
    ProtectHealer,
    TargetHealer,
    TargetTb,
    KillBoss,
    BotJungle,
    TopJungle,
    Bomb,
    SquadWiped
}

public class UserAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? SquadId { get; set; }
    public string? DiscordId { get; set; }
    public string? DiscordUsername { get; set; }
}

// Auth

public record LoginDto(string Username, string Password);

public record AuthResponseDto(bool Success, string? Message, string? Token, string? Username, UserRole? Role, string? SquadId);

public record LogoutDto(string Token);

// Client  Server 

public record ReportEventDto(EnemyRole? EnemyRole, string? Note);

public record IssueOrderDto(OrderType Type);

// Server  Client 

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

// Roster 

public record SquadRosterDto(
    string SquadId,
    string Side,
    string? LeaderName,
    List<string> Members
);

public record UpdateRosterDto(
    List<SquadRosterDto> Squads
);

// Accounts 

public record AccountSummaryDto(string Username, UserRole Role, string? SquadId, string? DiscordUsername);

public record UpsertAccountDto(string Username, UserRole Role, string? SquadId, string? Password);
public record ChangePasswordDto(string OldPassword, string NewPassword);
public record DiscordConfigDto(string ClientId, string RedirectUri);
public record MyAccountDto(string Username, UserRole Role, string? SquadId, bool DiscordLinked, string? DiscordUsername);

// Discord self-service login/registration 

public record DiscordLoginStartDto(string State);
public record DiscordRegisterDto(string PendingToken, string Nickname);

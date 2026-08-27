namespace GvGPoc.Models;

public enum UserRole
{
    Admin,
    Commander,
    Leader,
    Player,
    Developer
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
    DefendBot,
    DefendMid,
    DefendTop,
    DefendGoose,
    DefendTree,
    TargetHealer,
    TargetTb,
    KillBoss,
    BotJungle,
    TopJungle,
    Bomb
}

public enum SquadStatusType
{
    SquadWiped,
    Regroup,
    NeedHelp,
    Retreating,
    Autonomous
}

public enum MemberRole
{
    Dps,
    Tank,
    Healer
}


public enum MemberTag
{
    Jungle,
    Boss,
    Backup,
    JungleTopOwn,
    JungleBotOwn,
    JungleTopEnemy,
    JungleBotEnemy
}

public enum BulwarkPosition
{
    None,
    Bottom,
    Center,
    Top
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

    // Security info
    public DateTimeOffset? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
}

// Auth

public record LoginDto(string Username, string Password);

public record AuthResponseDto(bool Success, string? Message, string? Token, string? Username, UserRole? Role, string? SquadId);

public record LogoutDto(string Token);

// Client  Server 

public record ReportEventDto(EnemyRole? EnemyRole, string? Note);

public record IssueOrderDto(OrderType Type, List<string> TargetSquadIds);
public record ReportSquadStatusDto(SquadStatusType Type);

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

public record SquadStatusPushDto(
    Guid StatusId,
    string ReporterName,
    SquadStatusType Type,
    string TargetSquadId,
    DateTimeOffset Timestamp
);

// Battle

public record BattleStatusDto(bool IsActive, DateTimeOffset? StartedAt);

public record SpawnReminderDto(string Kind, int RemainingMinutes, DateTimeOffset FiredAt, string? TargetSquadId = null, List<MemberTag>? Tags = null);

public static class MemberTagHelpers
{
    public static readonly MemberTag[] SpecificJungleTags = {
        MemberTag.JungleTopOwn, MemberTag.JungleBotOwn, MemberTag.JungleTopEnemy, MemberTag.JungleBotEnemy
    };
}

// Roster 

// Reserves
public static class RosterConstants
{
    public const string ReserveSquadId = "RESERVE";
}

// Player role
public record SquadMemberDto(
    string Nickname,
    MemberRole Role,
    BulwarkPosition Bulwark,
    string? Build = null
);

public record SquadRosterDto(
    string SquadId,
    string Side,
    string? LeaderName,
    List<SquadMemberDto> Members,
    string? Label = null,
    List<MemberTag>? Tags = null
);

public record UpdateRosterDto(
    List<SquadRosterDto> Squads
);



public record MemberPresetDto(
    string Nickname,
    MemberRole Role,
    BulwarkPosition Bulwark,
    string? Build = null
);


public enum BattlePhase
{
    Phase1,
    Phase2,
    Phase3
}

// Discord webhooks

public record DiscordChannelDto(string Id, string Name, string WebhookUrl);
public record DiscordChannelSummaryDto(string Id, string Name);
public record UpsertDiscordChannelDto(string? Id, string Name, string WebhookUrl);
public record SendDiscordMessageDto(string ChannelId, string Message, List<string> PingNicknames);
public record SendDiscordMessageResultDto(bool Success, string? Message, int RealPings, int FallbackPings);

// Auto-generated message from the current roster
public record SendRosterMessageDto(string ChannelId, List<string>? SquadIds);


public record OrderMacroDto(string Id, string Name, OrderType Type, List<string> SquadIds);
public record UpsertOrderMacroDto(string? Id, string Name, OrderType Type, List<string>? SquadIds);


public record GoalOrderTargetDto(
    List<string>? Builds,
    List<MemberRole>? Roles,
    List<string>? Sides,
    List<string>? SquadIds
);

public record IssueGoalOrderDto(string Text, GoalOrderTargetDto? Target, int? TimerSeconds, BattlePhase? Phase);

public record GoalOrderPushDto(
    Guid GoalOrderId,
    string IssuerName,
    string Text,
    GoalOrderTargetDto? Target,
    int? TimerSeconds,
    BattlePhase? Phase,
    DateTimeOffset IssuedAt
);

public record GoalOrderMacroDto(string Id, string Name, string Text, GoalOrderTargetDto? Target, int? TimerSeconds, BattlePhase? Phase);
public record UpsertGoalOrderMacroDto(string? Id, string Name, string Text, GoalOrderTargetDto? Target, int? TimerSeconds, BattlePhase? Phase);

// Custom emoji
// ♜↑  ♜  ♜↓
public record RoleEmojiEntryDto(MemberRole Role, string? Emoji);
public record TagEmojiEntryDto(MemberTag Tag, string? Emoji);
public record EmojiSettingsDto(List<RoleEmojiEntryDto> Roles, List<TagEmojiEntryDto> Tags);

// Accounts 

// LastLoginAt/LastLoginIp 
public record AccountSummaryDto(string Username, UserRole Role, string? SquadId, string? DiscordUsername, DateTimeOffset? LastLoginAt = null, string? LastLoginIp = null);

public record UpsertAccountDto(string Username, UserRole Role, string? SquadId, string? Password);

public record RenameAccountDto(string OldUsername, string NewUsername);


public record AssignAccountRoleDto(string Username, UserRole Role, string? SquadId);

public record BulkAssignAccountRoleDto(List<AssignAccountRoleDto> Assignments);
public record BulkDeleteAccountsDto(List<string> Usernames);
public record BulkCreateAccountDto(string Username, string Password, UserRole Role, string? SquadId);
public record BulkCreateAccountsDto(List<BulkCreateAccountDto> Accounts);

public record BulkAccountErrorDto(string Username, string Error);
public record BulkAccountResultDto(List<string> Succeeded, List<BulkAccountErrorDto> Failed);
public record ChangePasswordDto(string OldPassword, string NewPassword);
public record DiscordConfigDto(string ClientId, string RedirectUri);
public record MyAccountDto(string Username, UserRole Role, string? SquadId, bool DiscordLinked, string? DiscordUsername);

// Discord self-service login/registration 

public record DiscordLoginStartDto(string State);
public record DiscordRegisterDto(string PendingToken, string Nickname);

// logs
public record LogFileSummaryDto(string FileName, DateTimeOffset SavedAt);

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
    Bomb
}

public enum SquadStatusType
{
    SquadWiped,
    Regrouped,
    NeedHealing,
    Retreating,
    ObjectiveSecured
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
    Backup
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
}

// Auth

public record LoginDto(string Username, string Password);

public record AuthResponseDto(bool Success, string? Message, string? Token, string? Username, UserRole? Role, string? SquadId);

public record LogoutDto(string Token);

// Client  Server 

public record ReportEventDto(EnemyRole? EnemyRole, string? Note);

public record IssueOrderDto(OrderType Type, string TargetSquadId);
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

// Roster 

// Резерв — не боевой отряд, а отдельный список запасных игроков. Хранится как обычный
// SquadRosterDto с этим фиксированным SquadId, чтобы переиспользовать всю логику
// назначения/снятия игроков, но UI (админка/панель командира) рисует его отдельно
// от боевых отрядов и не даёт выбрать его как цель приказа.
public static class RosterConstants
{
    public const string ReserveSquadId = "RESERVE";
}

// Роль игрока — на игроке. Теги (джунгли/босс/бэкап) теперь на команде, не на игроке.
public record SquadMemberDto(
    string Nickname,
    MemberRole Role,
    BulwarkPosition Bulwark
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

// Запоминание роли/булварка игрока между сборками ростера (теги теперь командные, не запоминаются по игроку)

public record MemberPresetDto(
    string Nickname,
    MemberRole Role,
    BulwarkPosition Bulwark
);

// Discord webhooks / каналы

public record DiscordChannelDto(string Id, string Name, string WebhookUrl);
public record DiscordChannelSummaryDto(string Id, string Name);
public record UpsertDiscordChannelDto(string? Id, string Name, string WebhookUrl);
public record SendDiscordMessageDto(string ChannelId, string Message, List<string> PingNicknames);
public record SendDiscordMessageResultDto(bool Success, string? Message, int RealPings, int FallbackPings);

// Автогенерация "Team composition" сообщения из текущего ростера, с реальными пингами по нику
public record SendRosterMessageDto(string ChannelId, List<string>? SquadIds);

// Кастомные эмодзи сервера для ролей и тегов (например <:tank:123456789012345678>).
// Пусто/не задано = дефолтный юникод-эмодзи. Значки булварка фиксированы (♜↑ / ♜ / ♜↓) и не настраиваются здесь.
public record RoleEmojiEntryDto(MemberRole Role, string? Emoji);
public record TagEmojiEntryDto(MemberTag Tag, string? Emoji);
public record EmojiSettingsDto(List<RoleEmojiEntryDto> Roles, List<TagEmojiEntryDto> Tags);

// Accounts 

public record AccountSummaryDto(string Username, UserRole Role, string? SquadId, string? DiscordUsername);

public record UpsertAccountDto(string Username, UserRole Role, string? SquadId, string? Password);
public record ChangePasswordDto(string OldPassword, string NewPassword);
public record DiscordConfigDto(string ClientId, string RedirectUri);
public record MyAccountDto(string Username, UserRole Role, string? SquadId, bool DiscordLinked, string? DiscordUsername);

// Discord self-service login/registration 

public record DiscordLoginStartDto(string State);
public record DiscordRegisterDto(string PendingToken, string Nickname);

// logs
public record LogFileSummaryDto(string FileName, DateTimeOffset SavedAt);

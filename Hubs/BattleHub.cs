using GvGPoc.Models;
using GvGPoc.State;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace GvGPoc.Hubs;

public class BattleHub : Hub
{
    private readonly IBattleState _state;
    private readonly ISessionStore _sessions;
    private readonly IAccountStore _accounts;
    private readonly HubActionRateLimiter _rateLimiter;
    private readonly IConnectionTracker _connections;
    private readonly IMemberPresetStore _presets;
    private readonly IDiscordWebhookStore _discordChannels;
    private readonly IEmojiSettingsStore _emojiSettings;
    private readonly IOrderMacroStore _macros;
    private readonly IHttpClientFactory _httpClientFactory;

    public BattleHub(IBattleState state, ISessionStore sessions, IAccountStore accounts, HubActionRateLimiter rateLimiter,
        IConnectionTracker connections, IMemberPresetStore presets, IDiscordWebhookStore discordChannels, IEmojiSettingsStore emojiSettings,
        IOrderMacroStore macros, IHttpClientFactory httpClientFactory)
    {
        _state = state;
        _sessions = sessions;
        _accounts = accounts;
        _rateLimiter = rateLimiter;
        _connections = connections;
        _presets = presets;
        _discordChannels = discordChannels;
        _emojiSettings = emojiSettings;
        _macros = macros;
        _httpClientFactory = httpClientFactory;
    }

    // SignalR groups
    private const string CommanderGroup = "role-commander";
    private const string AdminGroup = "role-admin";
    private static string SquadGroup(string squadId) => "squad-" + squadId;

    public override async Task OnConnectedAsync()
    {
        var token = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
        var session = string.IsNullOrEmpty(token) ? null : _sessions.Get(token);

        if (session is null)
        {
            Context.Abort();
            return;
        }

        Context.Items["session"] = session;
        _connections.Add(session.Username, Context.ConnectionId);

        var squadId = _accounts.Find(session.Username)?.SquadId;
        if (!string.IsNullOrEmpty(squadId))
            await Groups.AddToGroupAsync(Context.ConnectionId, SquadGroup(squadId));

        if (session.Role == UserRole.Commander)
            await Groups.AddToGroupAsync(Context.ConnectionId, CommanderGroup);
        if (session.Role is UserRole.Admin or UserRole.Developer)
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);

        await Clients.Caller.SendAsync("Snapshot", _state.GetRecentHistory());
        await Clients.Caller.SendAsync("RosterUpdated", _state.GetRoster());
        await Clients.Caller.SendAsync("BattleStatusChanged", _state.GetBattleStatus());
        await Clients.Caller.SendAsync("OnlineUsernames", _connections.GetOnlineUsernames());
        await base.OnConnectedAsync();

        // announce presence on the user's first connection 
        if (_connections.GetConnections(session.Username).Count == 1)
            await BroadcastPresence(session.Username, squadId, online: true);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _rateLimiter.Reset(Context.ConnectionId);
        var username = _connections.Remove(Context.ConnectionId);

        if (!string.IsNullOrEmpty(username) && !_connections.IsOnline(username))
        {
            var squadId = _accounts.Find(username)?.SquadId;
            await BroadcastPresence(username, squadId, online: false);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastPresence(string username, string? squadId, bool online)
    {
        var groups = new List<string> { CommanderGroup, AdminGroup };
        if (!string.IsNullOrEmpty(squadId)) groups.Add(SquadGroup(squadId));
        return Clients.Groups(groups).SendAsync("PresenceChanged", new { username, online });
    }


    public Task<List<string>> GetOnlineUsernames()
    {
        RequireAuthenticated();
        return Task.FromResult(_connections.GetOnlineUsernames().ToList());
    }

    public async Task ReportEvent(ReportEventDto dto)
    {
        var session = RequireRole(UserRole.Leader);
        var squadId = RequireSquad(session);
        RequireNotRateLimited();

        var evt = _state.AddEvent(dto, session.Username, squadId);
        await Clients.Groups(SquadGroup(squadId), CommanderGroup, AdminGroup).SendAsync("BattleEvent", evt);
    }

    public async Task IssueOrder(IssueOrderDto dto)
    {
        var session = RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);

        var targetSquadIds = (dto.TargetSquadIds ?? new()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (targetSquadIds.Count == 0)
            throw new HubException("Select at least one squad.");

        var roster = _state.GetRoster();
        foreach (var squadId in targetSquadIds)
        {
            if (string.Equals(squadId, RosterConstants.ReserveSquadId, StringComparison.OrdinalIgnoreCase))
                throw new HubException("Can't issue combat orders to Reserves.");
            if (!roster.Any(s => string.Equals(s.SquadId, squadId, StringComparison.OrdinalIgnoreCase)))
                throw new HubException($"Unknown squad: {squadId}.");
        }
        RequireNotRateLimited();

        // One order record per target squad 
        foreach (var squadId in targetSquadIds)
        {
            var order = _state.AddOrder(dto, session.Username, squadId);
            await Clients.Groups(SquadGroup(squadId), CommanderGroup, AdminGroup).SendAsync("OrderIssued", order);
        }
    }

    // Order macros

    public Task<List<OrderMacroDto>> ListOrderMacros()
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        return Task.FromResult(_macros.All().OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public Task<OrderMacroDto> UpsertOrderMacro(UpsertOrderMacroDto dto)
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new HubException("Give the macro a name.");

        var macro = _macros.Upsert(dto.Id, dto.Name.Trim(), dto.Type, dto.SquadIds);
        return Task.FromResult(macro);
    }

    public Task DeleteOrderMacro(string id)
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        _macros.Delete(id);
        return Task.CompletedTask;
    }

    public async Task ExecuteOrderMacro(string id)
    {
        var session = RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        var macro = _macros.Find(id) ?? throw new HubException("That macro doesn't exist anymore.");
        RequireNotRateLimited();

        var roster = _state.GetRoster();
        var nonReserveSquadIds = roster
            .Where(s => !string.Equals(s.SquadId, RosterConstants.ReserveSquadId, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.SquadId)
            .ToList();


        var targetSquadIds = (macro.SquadIds is { Count: > 0 } ? macro.SquadIds : nonReserveSquadIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(sid => nonReserveSquadIds.Contains(sid, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (targetSquadIds.Count == 0)
            throw new HubException("None of this macro's squads exist in the current roster.");

        var orderDto = new IssueOrderDto(macro.Type, targetSquadIds);
        foreach (var squadId in targetSquadIds)
        {
            var order = _state.AddOrder(orderDto, session.Username, squadId);
            await Clients.Groups(SquadGroup(squadId), CommanderGroup, AdminGroup).SendAsync("OrderIssued", order);
        }
    }

    public async Task ReportSquadStatus(ReportSquadStatusDto dto)
    {
        var session = RequireRole(UserRole.Leader);
        var squadId = RequireSquad(session);
        RequireNotRateLimited();

        var status = _state.AddSquadStatus(dto, session.Username, squadId);
        await Clients.Groups(SquadGroup(squadId), CommanderGroup, AdminGroup).SendAsync("SquadStatusChanged", status);
    }

    public async Task Sos()
    {
        var session = RequireRole(UserRole.Player, UserRole.Leader);
        var squadId = RequireSquad(session);
        RequireNotRateLimited();

        var evt = _state.AddSos(session.Username, squadId);
        await Clients.Groups(SquadGroup(squadId), CommanderGroup, AdminGroup).SendAsync("BattleEvent", evt);
    }

    public async Task UpdateRoster(UpdateRosterDto dto)
    {
        RequireRole(UserRole.Admin, UserRole.Developer);
        _state.SetRoster(dto.Squads);
        _presets.SaveMany(dto.Squads.SelectMany(s => s.Members));
        await SyncAccountSquadsWithRoster(dto.Squads);
        await Clients.All.SendAsync("RosterUpdated", dto.Squads);
    }

    // Remembered role by nickname
    public Task<Dictionary<string, MemberPresetDto>> GetMemberPresets()
    {
        RequireRole(UserRole.Admin, UserRole.Developer);
        return Task.FromResult(_presets.All().ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase));
    }


    private async Task SyncAccountSquadsWithRoster(IReadOnlyList<SquadRosterDto> squads)
    {
        var memberSquad = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var squad in squads)
            foreach (var member in squad.Members)
                memberSquad[member.Nickname] = squad.SquadId;

        foreach (var account in _accounts.All().ToList())
        {
            if (account.Role != UserRole.Leader && account.Role != UserRole.Player)
                continue; // Commander/Admin aren't tied to the roster

            var oldSquadId = account.SquadId;
            var newSquadId = memberSquad.TryGetValue(account.Username, out var sid) ? sid : null;
            if (string.Equals(oldSquadId, newSquadId, StringComparison.OrdinalIgnoreCase))
                continue;

            _accounts.Upsert(account.Username, account.Role, newSquadId, null);

            // SignalR groups for all connections of this account
            foreach (var connectionId in _connections.GetConnections(account.Username))
            {
                if (!string.IsNullOrEmpty(oldSquadId))
                    await Groups.RemoveFromGroupAsync(connectionId, SquadGroup(oldSquadId));
                if (!string.IsNullOrEmpty(newSquadId))
                    await Groups.AddToGroupAsync(connectionId, SquadGroup(newSquadId));
            }
        }
    }

    // Discord channels

    public Task<List<DiscordChannelDto>> ListDiscordChannels()
    {
        RequireRole(UserRole.Admin, UserRole.Developer);
        return Task.FromResult(_discordChannels.All().ToList());
    }

    public Task<List<DiscordChannelSummaryDto>> ListDiscordChannelNames()
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        var list = _discordChannels.All().Select(c => new DiscordChannelSummaryDto(c.Id, c.Name)).ToList();
        return Task.FromResult(list);
    }

    public Task<DiscordChannelDto> UpsertDiscordChannel(UpsertDiscordChannelDto dto)
    {
        RequireRole(UserRole.Admin, UserRole.Developer);
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new HubException("Channel name is required.");
        if (string.IsNullOrWhiteSpace(dto.WebhookUrl) || !dto.WebhookUrl.StartsWith("https://discord.com/api/webhooks/", StringComparison.OrdinalIgnoreCase))
            throw new HubException("That doesn't look like a valid Discord webhook URL.");

        var channel = _discordChannels.Upsert(dto.Id, dto.Name.Trim(), dto.WebhookUrl.Trim());
        return Task.FromResult(channel);
    }

    public Task DeleteDiscordChannel(string id)
    {
        RequireRole(UserRole.Admin, UserRole.Developer);
        _discordChannels.Delete(id);
        return Task.CompletedTask;
    }

    // Sends a message to Discord 
    public async Task<SendDiscordMessageResultDto> SendDiscordMessage(SendDiscordMessageDto dto)
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);

        var channel = _discordChannels.Find(dto.ChannelId);
        if (channel is null) throw new HubException("Unknown Discord channel.");
        if (string.IsNullOrWhiteSpace(dto.Message) && (dto.PingNicknames is null || dto.PingNicknames.Count == 0))
            throw new HubException("Nothing to send.");

        var mentionIds = new List<string>();
        var fallbackNames = new List<string>();

        foreach (var nickname in (dto.PingNicknames ?? new()).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var account = _accounts.Find(nickname);
            if (account?.DiscordId is not null)
                mentionIds.Add(account.DiscordId);
            else
                fallbackNames.Add(nickname);
        }

        var contentParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(dto.Message)) contentParts.Add(dto.Message.Trim());

        var pingParts = new List<string>();
        pingParts.AddRange(mentionIds.Select(id => $"<@{id}>"));
        pingParts.AddRange(fallbackNames.Select(n => $"@{n}"));
        if (pingParts.Count > 0) contentParts.Add(string.Join(" ", pingParts));

        var content = string.Join("\n\n", contentParts);
        if (content.Length > 1900) content = content[..1900] + "…";

        var payload = new {
            content,
            allowed_mentions = new { parse = Array.Empty<string>(), users = mentionIds }
        };

        var http = _httpClientFactory.CreateClient();
        var response = await http.PostAsJsonAsync(channel.WebhookUrl, payload);

        if (!response.IsSuccessStatusCode)
            return new SendDiscordMessageResultDto(false, $"Discord returned {(int)response.StatusCode}.", mentionIds.Count, fallbackNames.Count);

        return new SendDiscordMessageResultDto(true, null, mentionIds.Count, fallbackNames.Count);
    }

    // Team composition message from the current roster 
    public async Task<SendDiscordMessageResultDto> SendRosterMessage(SendRosterMessageDto dto)
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);

        var channel = _discordChannels.Find(dto.ChannelId);
        if (channel is null) throw new HubException("Unknown Discord channel.");

        var roster = _state.GetRoster();
        var squads = (dto.SquadIds is { Count: > 0 })
            ? roster.Where(s => dto.SquadIds.Contains(s.SquadId, StringComparer.OrdinalIgnoreCase)).ToList()
            : roster.Where(s => s.Members.Count > 0).ToList();

        squads = squads.Where(s => s.Members.Count > 0).ToList();
        if (squads.Count == 0) throw new HubException("Nothing to send, the selected squads have no members.");

        var mentionIds = new List<string>();
        var fallbackNames = new List<string>();
        var lines = new List<string> { "**Team composition:**", "" };

        var teamIndex = 0;
        foreach (var squad in squads)
        {
            teamIndex++;
            var label = string.IsNullOrWhiteSpace(squad.Label) ? squad.SquadId : squad.Label;
            var tagBadges = (squad.Tags ?? new()).Count > 0
                ? " " + string.Join(" ", (squad.Tags ?? new()).Select(t => $"{_emojiSettings.TagEmoji(t)}{t}"))
                : "";
            lines.Add($"**Team {teamIndex} ({label}){tagBadges}:**");

            foreach (var member in squad.Members)
            {
                var account = _accounts.Find(member.Nickname);
                string mention;
                if (account?.DiscordId is not null)
                {
                    mentionIds.Add(account.DiscordId);
                    mention = $"<@{account.DiscordId}>";
                }
                else
                {
                    fallbackNames.Add(member.Nickname);
                    mention = $"@{member.Nickname}";
                }

                var bulwarkText = BulwarkSymbol(member.Bulwark);
                lines.Add($"{_emojiSettings.RoleEmoji(member.Role)} {mention}{bulwarkText}");
            }
            lines.Add("");
        }

        var content = string.Join("\n", lines).TrimEnd();
        if (content.Length > 1900) content = content[..1900] + "…";

        var payload = new {
            content,
            allowed_mentions = new { parse = Array.Empty<string>(), users = mentionIds.Distinct().ToList() }
        };

        var http = _httpClientFactory.CreateClient();
        var response = await http.PostAsJsonAsync(channel.WebhookUrl, payload);

        if (!response.IsSuccessStatusCode)
            return new SendDiscordMessageResultDto(false, $"Discord returned {(int)response.StatusCode}.", mentionIds.Count, fallbackNames.Count);

        return new SendDiscordMessageResultDto(true, null, mentionIds.Count, fallbackNames.Count);
    }

    // ♜↑  ♜  ♜↓ 
    private static string BulwarkSymbol(BulwarkPosition pos) => pos switch {
        BulwarkPosition.Top => " ♜↑",
        BulwarkPosition.Center => " ♜",
        BulwarkPosition.Bottom => " ♜↓",
        _ => ""
    };

    // Custom server emoji 

    public Task<EmojiSettingsDto> GetEmojiSettings()
    {
        RequireAuthenticated();
        return Task.FromResult(_emojiSettings.Get());
    }

    public Task<EmojiSettingsDto> UpdateEmojiSettings(EmojiSettingsDto dto)
    {
        RequireRole(UserRole.Admin, UserRole.Developer);
        return Task.FromResult(_emojiSettings.Update(dto));
    }

    public async Task StartBattle()
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        var status = _state.StartBattle();
        await Clients.All.SendAsync("BattleStatusChanged", status);
    }

    
    // log and clearing
    public async Task EndBattle()
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        _state.EndBattle();
        await Clients.All.SendAsync("HistoryReset");
        await Clients.All.SendAsync("BattleStatusChanged", _state.GetBattleStatus());
    }

    public Task<string> SaveLogSnapshot()
    {
        RequireRole(UserRole.Admin, UserRole.Developer);
        return Task.FromResult(_state.SaveLogSnapshot());
    }

    public Task<IReadOnlyList<LogFileSummaryDto>> ListLogFiles()
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        return Task.FromResult(_state.ListLogFiles());
    }

    public Task<string> GetLogFile(string fileName)
    {
        RequireRole(UserRole.Commander, UserRole.Admin, UserRole.Developer);
        var content = _state.ReadLogFile(fileName);
        if (content is null) throw new HubException("Log file not found.");
        return Task.FromResult(content);
    }

    public Task<List<AccountSummaryDto>> ListAccounts()
    {
        var session = RequireRole(UserRole.Admin, UserRole.Developer);
        // Security info 
        var isDeveloper = session.Role == UserRole.Developer;
        var summaries = _accounts.All()
            .Select(a => new AccountSummaryDto(
                a.Username, a.Role, a.SquadId, a.DiscordUsername,
                isDeveloper ? a.LastLoginAt : null,
                isDeveloper ? a.LastLoginIp : null))
            .OrderBy(a => a.Role).ThenBy(a => a.Username)
            .ToList();
        return Task.FromResult(summaries);
    }


    public Task UpsertAccount(UpsertAccountDto dto)
    {
        RequireRole(UserRole.Developer);
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new HubException("Username is required.");

        _accounts.Upsert(dto.Username.Trim(), dto.Role, dto.SquadId, dto.Password);
        return Task.CompletedTask;
    }


    public Task AssignAccountRole(AssignAccountRoleDto dto)
    {
        var session = RequireRole(UserRole.Admin, UserRole.Developer);
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new HubException("Username is required.");

        var target = _accounts.Find(dto.Username.Trim());
        if (target is null)
            throw new HubException("That account isn't registered yet. Only already-registered accounts can be assigned a role.");

        if (session.Role == UserRole.Admin && target.Role is UserRole.Admin or UserRole.Developer)
            throw new HubException("Admin and Developer accounts can only be changed by a Developer.");

        if (session.Role == UserRole.Admin && dto.Role is UserRole.Admin or UserRole.Developer)
            throw new HubException("Only a Developer can grant Admin or Developer access.");

        _accounts.AssignRoleAndSquad(dto.Username.Trim(), dto.Role, dto.SquadId);
        return Task.CompletedTask;
    }



    public Task<BulkAccountResultDto> BulkAssignAccountRole(BulkAssignAccountRoleDto dto)
    {
        var session = RequireRole(UserRole.Admin, UserRole.Developer);
        var succeeded = new List<string>();
        var failed = new List<BulkAccountErrorDto>();

        foreach (var item in dto.Assignments ?? new())
        {
            var username = (item.Username ?? "").Trim();
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    throw new HubException("Username is required.");
                var target = _accounts.Find(username);
                if (target is null)
                    throw new HubException("Not registered yet - only already-registered accounts can be assigned.");
                if (session.Role == UserRole.Admin && target.Role is UserRole.Admin or UserRole.Developer)
                    throw new HubException("Admin and Developer accounts can only be changed by a Developer.");
                if (session.Role == UserRole.Admin && item.Role is UserRole.Admin or UserRole.Developer)
                    throw new HubException("Only a Developer can grant Admin or Developer access.");

                _accounts.AssignRoleAndSquad(username, item.Role, item.SquadId);
                succeeded.Add(username);
            }
            catch (Exception ex)
            {
                failed.Add(new BulkAccountErrorDto(string.IsNullOrWhiteSpace(username) ? "(blank)" : username, ex.Message));
            }
        }
        return Task.FromResult(new BulkAccountResultDto(succeeded, failed));
    }

    public Task<BulkAccountResultDto> BulkDeleteAccounts(BulkDeleteAccountsDto dto)
    {
        RequireRole(UserRole.Developer);
        var succeeded = new List<string>();
        var failed = new List<BulkAccountErrorDto>();

        foreach (var raw in dto.Usernames ?? new())
        {
            var username = (raw ?? "").Trim();
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    throw new HubException("Username is required.");
                if (ProtectedAccounts.Contains(username))
                    throw new HubException("This account is protected and cannot be deleted.");
                if (_accounts.Find(username) is null)
                    throw new HubException("Account not found.");

                _accounts.Delete(username);
                succeeded.Add(username);
            }
            catch (Exception ex)
            {
                failed.Add(new BulkAccountErrorDto(string.IsNullOrWhiteSpace(username) ? "(blank)" : username, ex.Message));
            }
        }
        return Task.FromResult(new BulkAccountResultDto(succeeded, failed));
    }


    public Task<BulkAccountResultDto> BulkCreateAccounts(BulkCreateAccountsDto dto)
    {
        RequireRole(UserRole.Developer);
        var succeeded = new List<string>();
        var failed = new List<BulkAccountErrorDto>();

        foreach (var item in dto.Accounts ?? new())
        {
            var username = (item.Username ?? "").Trim();
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    throw new HubException("Username is required.");
                if (string.IsNullOrWhiteSpace(item.Password) || item.Password.Length < 4)
                    throw new HubException("Password is too short.");

                _accounts.Upsert(username, item.Role, item.SquadId, item.Password);
                succeeded.Add(username);
            }
            catch (Exception ex)
            {
                failed.Add(new BulkAccountErrorDto(string.IsNullOrWhiteSpace(username) ? "(blank)" : username, ex.Message));
            }
        }
        return Task.FromResult(new BulkAccountResultDto(succeeded, failed));
    }

    public Task<MyAccountDto> GetMyAccount()
    {
        var session = RequireAuthenticated();
        var account = _accounts.Find(session.Username);
        return Task.FromResult(new MyAccountDto(
            session.Username,
            session.Role,
            session.SquadId,
            account?.DiscordId is not null,
            account?.DiscordUsername
        ));
    }

    public Task ChangePassword(ChangePasswordDto dto)
    {
        var session = RequireAuthenticated();
        var account = _accounts.Find(session.Username);
        if (account is null) throw new HubException("Account not found.");
        if (!_accounts.VerifyPassword(account, dto.OldPassword))
            throw new HubException("Current password is incorrect.");
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 4)
            throw new HubException("New password is too short.");

        _accounts.Upsert(account.Username, account.Role, account.SquadId, dto.NewPassword);
        return Task.CompletedTask;
    }

    // Accounts that can't be deleted
    private static readonly HashSet<string> ProtectedAccounts = new(StringComparer.OrdinalIgnoreCase) { "morda", "admin" };

    public Task DeleteAccount(string username)
    {
        RequireRole(UserRole.Developer);
        if (ProtectedAccounts.Contains(username?.Trim() ?? string.Empty))
            throw new HubException("This account is protected and cannot be deleted.");
        _accounts.Delete(username);
        return Task.CompletedTask;
    }

    private SessionInfo RequireRole(params UserRole[] allowedRoles)
    {
        if (Context.Items.TryGetValue("session", out var raw) && raw is SessionInfo session)
        {
            if (allowedRoles.Contains(session.Role)) return session;
            throw new HubException($"Your account role ({session.Role}) doesn't have access to this action.");
        }
        throw new HubException("Not authenticated.");
    }

    private SessionInfo RequireAuthenticated()
    {
        if (Context.Items.TryGetValue("session", out var raw) && raw is SessionInfo session) return session;
        throw new HubException("Not authenticated.");
    }


    private string RequireSquad(SessionInfo session)
    {
        var squadId = _accounts.Find(session.Username)?.SquadId;
        if (string.IsNullOrEmpty(squadId))
            throw new HubException("Your account isn't assigned to a squad yet, ask your admin.");
        return squadId;
    }

    private void RequireNotRateLimited()
    {
        if (!_rateLimiter.Allow(Context.ConnectionId))
            throw new HubException("You're sending actions too fast, slow down a bit.");
    }
}
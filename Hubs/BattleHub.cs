using GvGPoc.Models;
using GvGPoc.State;
using Microsoft.AspNetCore.SignalR;

namespace GvGPoc.Hubs;

public class BattleHub : Hub
{
    private readonly IBattleState _state;
    private readonly ISessionStore _sessions;
    private readonly IAccountStore _accounts;
    private readonly HubActionRateLimiter _rateLimiter;
    private readonly IConnectionTracker _connections;

    public BattleHub(IBattleState state, ISessionStore sessions, IAccountStore accounts, HubActionRateLimiter rateLimiter, IConnectionTracker connections)
    {
        _state = state;
        _sessions = sessions;
        _accounts = accounts;
        _rateLimiter = rateLimiter;
        _connections = connections;
    }

    // Группы SignalR
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
        if (session.Role == UserRole.Admin)
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);

        await Clients.Caller.SendAsync("Snapshot", _state.GetRecentHistory());
        await Clients.Caller.SendAsync("RosterUpdated", _state.GetRoster());
        await Clients.Caller.SendAsync("BattleStatusChanged", _state.GetBattleStatus());
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _rateLimiter.Reset(Context.ConnectionId);
        _connections.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
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
        var session = RequireRole(UserRole.Leader);
        var squadId = RequireSquad(session);
        RequireNotRateLimited();

        var order = _state.AddOrder(dto, session.Username, squadId);
        await Clients.Groups(SquadGroup(squadId), CommanderGroup, AdminGroup).SendAsync("OrderIssued", order);
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
        RequireRole(UserRole.Admin);
        _state.SetRoster(dto.Squads);
        await SyncAccountSquadsWithRoster(dto.Squads);
        await Clients.All.SendAsync("RosterUpdated", dto.Squads);
    }


    private async Task SyncAccountSquadsWithRoster(IReadOnlyList<SquadRosterDto> squads)
    {
        var memberSquad = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var squad in squads)
            foreach (var member in squad.Members)
                memberSquad[member] = squad.SquadId;

        foreach (var account in _accounts.All().ToList())
        {
            if (account.Role != UserRole.Leader && account.Role != UserRole.Player)
                continue; // Commander/Admin не привязаны к ростеру

            var oldSquadId = account.SquadId;
            var newSquadId = memberSquad.TryGetValue(account.Username, out var sid) ? sid : null;
            if (string.Equals(oldSquadId, newSquadId, StringComparison.OrdinalIgnoreCase))
                continue;

            _accounts.Upsert(account.Username, account.Role, newSquadId, null);

            // смена групп SignalR для всех подключений этого аккаунта
            foreach (var connectionId in _connections.GetConnections(account.Username))
            {
                if (!string.IsNullOrEmpty(oldSquadId))
                    await Groups.RemoveFromGroupAsync(connectionId, SquadGroup(oldSquadId));
                if (!string.IsNullOrEmpty(newSquadId))
                    await Groups.AddToGroupAsync(connectionId, SquadGroup(newSquadId));
            }
        }
    }

    public async Task StartBattle()
    {
        RequireRole(UserRole.Admin);
        var status = _state.StartBattle();
        await Clients.All.SendAsync("BattleStatusChanged", status);
    }

    
    // лог и очистка
    public async Task EndBattle()
    {
        RequireRole(UserRole.Admin);
        _state.EndBattle();
        await Clients.All.SendAsync("HistoryReset");
        await Clients.All.SendAsync("BattleStatusChanged", _state.GetBattleStatus());
    }

    public Task<string> SaveLogSnapshot()
    {
        RequireRole(UserRole.Admin);
        return Task.FromResult(_state.SaveLogSnapshot());
    }

    public Task<List<AccountSummaryDto>> ListAccounts()
    {
        RequireRole(UserRole.Admin);
        var summaries = _accounts.All()
            .Select(a => new AccountSummaryDto(a.Username, a.Role, a.SquadId, a.DiscordUsername))
            .OrderBy(a => a.Role).ThenBy(a => a.Username)
            .ToList();
        return Task.FromResult(summaries);
    }

    public Task UpsertAccount(UpsertAccountDto dto)
    {
        RequireRole(UserRole.Admin);
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new HubException("Username is required.");

        _accounts.Upsert(dto.Username.Trim(), dto.Role, dto.SquadId, dto.Password);
        return Task.CompletedTask;
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

    // Аккаунты которые нельзя удалить
    private static readonly HashSet<string> ProtectedAccounts = new(StringComparer.OrdinalIgnoreCase) { "morda", "admin" };

    public Task DeleteAccount(string username)
    {
        RequireRole(UserRole.Admin);
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
            throw new HubException("Your account isn't assigned to a squad yet — ask your admin.");
        return squadId;
    }

    private void RequireNotRateLimited()
    {
        if (!_rateLimiter.Allow(Context.ConnectionId))
            throw new HubException("You're sending actions too fast — slow down a bit.");
    }
}
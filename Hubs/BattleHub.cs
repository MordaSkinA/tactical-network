using GvGPoc.Models;
using GvGPoc.State;
using Microsoft.AspNetCore.SignalR;

namespace GvGPoc.Hubs;

// POC: broadcast всем подключённым клиентам (Clients.All), без групп по squad/side.
// В реальной системе routing по группам (см. архитектурный документ, раздел 8) —
// следующий шаг после того, как подтвердится сама механика.
public class BattleHub : Hub
{
    private readonly IBattleState _state;
    private readonly string _adminKey;

    public BattleHub(IBattleState state, IConfiguration config)
    {
        _state = state;
        // POC-only "калитка": один общий пароль на всю гильдию, не настоящая авторизация.
        // В Phase 1 заменяется на JWT + роль Admin, привязанную к конкретному Battle.
        _adminKey = config["AdminKey"] ?? "gvg-admin";
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Snapshot", _state.GetRecentHistory());
        await Clients.Caller.SendAsync("RosterUpdated", _state.GetRoster());
        await base.OnConnectedAsync();
    }

    public async Task ReportEvent(ReportEventDto dto)
    {
        var evt = _state.AddEvent(dto);
        await Clients.All.SendAsync("BattleEvent", evt);
    }

    public async Task IssueOrder(IssueOrderDto dto)
    {
        var order = _state.AddOrder(dto);
        await Clients.All.SendAsync("OrderIssued", order);
    }

    public async Task Sos(SosDto dto)
    {
        var evt = _state.AddSos(dto);
        await Clients.All.SendAsync("BattleEvent", evt);
    }

    public async Task UpdateRoster(UpdateRosterDto dto)
    {
        RequireAdmin(dto.AdminKey);
        _state.SetRoster(dto.Squads);
        await Clients.All.SendAsync("RosterUpdated", dto.Squads);
    }

    public async Task ResetHistory(AdminActionDto dto)
    {
        RequireAdmin(dto.AdminKey);
        _state.ResetHistory();
        await Clients.All.SendAsync("HistoryReset");
    }

    private void RequireAdmin(string providedKey)
    {
        if (providedKey != _adminKey)
        {
            // HubException — единственный тип, чей текст SignalR доносит до клиента
            // даже без EnableDetailedErrors, поэтому используем его для ожидаемых
            // (не багов) отказов вроде неверного пароля.
            throw new HubException("Invalid admin key.");
        }
    }
}

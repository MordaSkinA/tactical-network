using GvGPoc.Models;
using GvGPoc.State;
using Microsoft.AspNetCore.SignalR;

namespace GvGPoc.Hubs;

public class BattleHub : Hub
{
    private readonly IBattleState _state;
    private readonly string _adminKey;

    public BattleHub(IBattleState state, IConfiguration config)
    {
        _state = state;
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
            throw new HubException("Invalid admin key.");
        }
    }
}

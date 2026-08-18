using GvGPoc.Models;
using GvGPoc.State;
using Microsoft.AspNetCore.SignalR;

namespace GvGPoc.Hubs;

public class BattleHub : Hub
{
    private readonly IBattleState _state;

    public BattleHub(IBattleState state)
    {
        _state = state;
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Snapshot", _state.GetRecentHistory());
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
}

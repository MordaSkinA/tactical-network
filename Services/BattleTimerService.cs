using Microsoft.AspNetCore.SignalR;
using GvGPoc.Hubs;
using GvGPoc.Models;
using GvGPoc.State;

namespace GvGPoc.Services;

public class BattleTimerService : BackgroundService
{
    private const int RoundDurationMinutes = 30;
    private static readonly int[] JungleElapsedMinutes = { 5, 10, 15, 20, 25, 30 };
    private static readonly int[] BossElapsedMinutes = { 5, 15 };


    private const string CommanderGroup = "role-commander";
    private const string AdminGroup = "role-admin";
    private static string SquadGroup(string squadId) => "squad-" + squadId;

    private readonly IBattleState _state;
    private readonly IHubContext<BattleHub> _hub;
    private readonly ILogger<BattleTimerService> _logger;

    private DateTimeOffset? _lastSeenStart;
    private readonly HashSet<int> _firedThisBattle = new();

    public BattleTimerService(IBattleState state, IHubContext<BattleHub> hub, ILogger<BattleTimerService> logger)
    {
        _state = state;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var status = _state.GetBattleStatus();

                if (status.IsActive && status.StartedAt is DateTimeOffset startedAt)
                {
                    if (_lastSeenStart != startedAt)
                    {
                        _lastSeenStart = startedAt;
                        _firedThisBattle.Clear();
                    }

                    var elapsedMinutes = (int)(DateTimeOffset.UtcNow - startedAt).TotalMinutes;
                    var isJungle = Array.IndexOf(JungleElapsedMinutes, elapsedMinutes) >= 0;
                    var isBoss = Array.IndexOf(BossElapsedMinutes, elapsedMinutes) >= 0;

                    if ((isJungle || isBoss) && _firedThisBattle.Add(elapsedMinutes))
                    {
                        var remaining = RoundDurationMinutes - elapsedMinutes;
                        var firedAt = DateTimeOffset.UtcNow;

                        if (isBoss)
                        {

                            var bossDto = new SpawnReminderDto("Boss", remaining, firedAt);
                            await _hub.Clients.All.SendAsync("SpawnReminder", bossDto, stoppingToken);
                        }

                        if (isJungle)
                            await SendJungleReminders(remaining, firedAt, stoppingToken);
                    }
                }
                else
                {
                    _lastSeenStart = null;
                    _firedThisBattle.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BattleTimerService tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }


    private async Task SendJungleReminders(int remaining, DateTimeOffset firedAt, CancellationToken ct)
    {
        var roster = _state.GetRoster()
            .Where(s => !string.Equals(s.SquadId, RosterConstants.ReserveSquadId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var squadsWithSpecificTags = roster
            .Select(s => (Squad: s, Tags: (s.Tags ?? new()).Where(t => MemberTagHelpers.SpecificJungleTags.Contains(t)).ToList()))
            .Where(x => x.Tags.Count > 0)
            .ToList();

        if (squadsWithSpecificTags.Count == 0)
        {
            var fallbackDto = new SpawnReminderDto("Jungle", remaining, firedAt);
            await _hub.Clients.All.SendAsync("SpawnReminder", fallbackDto, ct);
            return;
        }

        foreach (var (squad, tags) in squadsWithSpecificTags)
        {
            var dto = new SpawnReminderDto("Jungle", remaining, firedAt, squad.SquadId, tags);
            await _hub.Clients.Groups(new[] { SquadGroup(squad.SquadId), CommanderGroup, AdminGroup }).SendAsync("SpawnReminder", dto, ct);
        }
    }
}
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
                        var kind = isJungle && isBoss ? "JungleAndBoss" : isJungle ? "Jungle" : "Boss";
                        var dto = new SpawnReminderDto(kind, RoundDurationMinutes - elapsedMinutes, DateTimeOffset.UtcNow);
                        await _hub.Clients.All.SendAsync("SpawnReminder", dto, stoppingToken);
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
}
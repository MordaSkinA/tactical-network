using System.Text.Json;
using System.Text.Json.Serialization;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IGoalOrderMacroStore
{
    IReadOnlyList<GoalOrderMacroDto> All();
    GoalOrderMacroDto? Find(string id);
    Task<GoalOrderMacroDto> UpsertAsync(string? id, string name, string text, GoalOrderTargetDto? target, int? timerSeconds, BattlePhase? phase);
    Task DeleteAsync(string id);
}


public class FileGoalOrderMacroStore : IGoalOrderMacroStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private List<GoalOrderMacroDto> _macros;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FileGoalOrderMacroStore(IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "goal-order-macros.json");
        _macros = Load();
    }

    private List<GoalOrderMacroDto> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<GoalOrderMacroDto>>(json, Options) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private async Task SaveAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_macros, Options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public IReadOnlyList<GoalOrderMacroDto> All() => _macros;

    public GoalOrderMacroDto? Find(string id) => _macros.FirstOrDefault(m => m.Id == id);

    public async Task<GoalOrderMacroDto> UpsertAsync(string? id, string name, string text, GoalOrderTargetDto? target, int? timerSeconds, BattlePhase? phase)
    {
        var macro = new GoalOrderMacroDto(
            string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id,
            name,
            text,
            target,
            timerSeconds,
            phase
        );
        _macros = _macros.Where(m => m.Id != macro.Id).Append(macro).ToList();
        await SaveAsync();
        return macro;
    }

    public async Task DeleteAsync(string id)
    {
        _macros = _macros.Where(m => m.Id != id).ToList();
        await SaveAsync();
    }
}

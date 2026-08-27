using System.Text.Json;
using System.Text.Json.Serialization;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IGoalOrderMacroStore
{
    IReadOnlyList<GoalOrderMacroDto> All();
    GoalOrderMacroDto? Find(string id);
    GoalOrderMacroDto Upsert(string? id, string name, string text, GoalOrderTargetDto? target, int? timerSeconds, BattlePhase? phase);
    void Delete(string id);
}


public class FileGoalOrderMacroStore : IGoalOrderMacroStore
{
    private readonly string _filePath;
    private readonly object _fileLock = new();
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

    private void Save()
    {
        lock (_fileLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_macros, Options);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
            }
        }
    }

    public IReadOnlyList<GoalOrderMacroDto> All() => _macros;

    public GoalOrderMacroDto? Find(string id) => _macros.FirstOrDefault(m => m.Id == id);

    public GoalOrderMacroDto Upsert(string? id, string name, string text, GoalOrderTargetDto? target, int? timerSeconds, BattlePhase? phase)
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
        Save();
        return macro;
    }

    public void Delete(string id)
    {
        _macros = _macros.Where(m => m.Id != id).ToList();
        Save();
    }
}

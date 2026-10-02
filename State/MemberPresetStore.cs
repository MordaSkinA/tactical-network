using System.Text.Json;
using System.Text.Json.Serialization;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IMemberPresetStore
{
    MemberPresetDto? Find(string nickname);
    IReadOnlyDictionary<string, MemberPresetDto> All();
    Task SaveAsync(SquadMemberDto member);
    Task SaveManyAsync(IEnumerable<SquadMemberDto> members);
}




public class FileMemberPresetStore : IMemberPresetStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Dictionary<string, MemberPresetDto> _presets;

    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public FileMemberPresetStore(IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "member-presets.json");
        _presets = Load();
    }

    private Dictionary<string, MemberPresetDto> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new(StringComparer.OrdinalIgnoreCase);
            var json = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<MemberPresetDto>>(json, Options) ?? new();
            return list.ToDictionary(p => p.Nickname, p => p, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task SaveAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_presets.Values.ToList(), Options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public MemberPresetDto? Find(string nickname) =>
        _presets.TryGetValue(nickname, out var p) ? p : null;

    public IReadOnlyDictionary<string, MemberPresetDto> All() => _presets;

    public async Task SaveAsync(SquadMemberDto member)
    {
        await _semaphore.WaitAsync();
        try
        {
            _presets[member.Nickname] = new MemberPresetDto(member.Nickname, member.Role, member.Bulwark, member.Build);
            var json = JsonSerializer.Serialize(_presets.Values.ToList(), Options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveManyAsync(IEnumerable<SquadMemberDto> members)
    {
        await _semaphore.WaitAsync();
        try
        {
            foreach (var m in members)
                _presets[m.Nickname] = new MemberPresetDto(m.Nickname, m.Role, m.Bulwark, m.Build);
            var json = JsonSerializer.Serialize(_presets.Values.ToList(), Options);
            await File.WriteAllTextAsync(_filePath, json);
        }
        catch
        {
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

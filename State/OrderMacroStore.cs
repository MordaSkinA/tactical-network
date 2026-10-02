using System.Text.Json;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IOrderMacroStore
{
    IReadOnlyList<OrderMacroDto> All();
    OrderMacroDto? Find(string id);
    Task<OrderMacroDto> UpsertAsync(string? id, string name, OrderType type, List<string>? squadIds);
    Task DeleteAsync(string id);
}

public class FileOrderMacroStore : IOrderMacroStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private List<OrderMacroDto> _macros;

    public FileOrderMacroStore(IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "order-macros.json");
        _macros = Load();
    }

    private List<OrderMacroDto> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<OrderMacroDto>>(json) ?? new();
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
            var json = JsonSerializer.Serialize(_macros, new JsonSerializerOptions { WriteIndented = true });
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

    public IReadOnlyList<OrderMacroDto> All() => _macros;

    public OrderMacroDto? Find(string id) => _macros.FirstOrDefault(m => m.Id == id);

    public async Task<OrderMacroDto> UpsertAsync(string? id, string name, OrderType type, List<string>? squadIds)
    {
        var macro = new OrderMacroDto(
            string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id,
            name,
            type,
            squadIds ?? new()
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

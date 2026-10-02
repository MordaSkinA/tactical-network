using System.Text.Json;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IDiscordWebhookStore
{
    IReadOnlyList<DiscordChannelDto> All();
    DiscordChannelDto? Find(string id);
    Task<DiscordChannelDto> UpsertAsync(string? id, string name, string webhookUrl);
    Task DeleteAsync(string id);
}


public class FileDiscordWebhookStore : IDiscordWebhookStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private List<DiscordChannelDto> _channels;

    public FileDiscordWebhookStore(IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "discord-webhooks.json");
        _channels = Load();
    }

    private List<DiscordChannelDto> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new();
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<DiscordChannelDto>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private async Task SaveAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(_channels, new JsonSerializerOptions { WriteIndented = true });
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

    public IReadOnlyList<DiscordChannelDto> All() => _channels;

    public DiscordChannelDto? Find(string id) => _channels.FirstOrDefault(c => c.Id == id);

    public async Task<DiscordChannelDto> UpsertAsync(string? id, string name, string webhookUrl)
    {
        var channel = new DiscordChannelDto(
            string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id,
            name,
            webhookUrl
        );
        _channels = _channels.Where(c => c.Id != channel.Id).Append(channel).ToList();
        await SaveAsync();
        return channel;
    }

    public async Task DeleteAsync(string id)
    {
        _channels = _channels.Where(c => c.Id != id).ToList();
        await SaveAsync();
    }
}

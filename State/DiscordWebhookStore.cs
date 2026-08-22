using System.Text.Json;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IDiscordWebhookStore
{
    IReadOnlyList<DiscordChannelDto> All();
    DiscordChannelDto? Find(string id);
    DiscordChannelDto Upsert(string? id, string name, string webhookUrl);
    void Delete(string id);
}

// Каналы Discord, куда можно слать сообщения по webhook-ссылке (без бота)
public class FileDiscordWebhookStore : IDiscordWebhookStore
{
    private readonly string _filePath;
    private readonly object _fileLock = new();
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

    private void Save()
    {
        lock (_fileLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(_channels, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
            }
        }
    }

    public IReadOnlyList<DiscordChannelDto> All() => _channels;

    public DiscordChannelDto? Find(string id) => _channels.FirstOrDefault(c => c.Id == id);

    public DiscordChannelDto Upsert(string? id, string name, string webhookUrl)
    {
        var channel = new DiscordChannelDto(
            string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id,
            name,
            webhookUrl
        );
        _channels = _channels.Where(c => c.Id != channel.Id).Append(channel).ToList();
        Save();
        return channel;
    }

    public void Delete(string id)
    {
        _channels = _channels.Where(c => c.Id != id).ToList();
        Save();
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using GvGPoc.Models;
using Microsoft.Extensions.Hosting;

namespace GvGPoc.State;

public interface IEmojiSettingsStore
{
    EmojiSettingsDto Get();
    EmojiSettingsDto Update(EmojiSettingsDto dto);
    string RoleEmoji(MemberRole role);
    string TagEmoji(MemberTag tag);
}

// Кастомные эмодзи сервера (Discord) для ролей и тегов команды, задаются в админке и сохраняются на диске
public class FileEmojiSettingsStore : IEmojiSettingsStore
{
    private readonly string _filePath;
    private readonly object _fileLock = new();
    private Dictionary<MemberRole, string> _roleEmoji;
    private Dictionary<MemberTag, string> _tagEmoji;

    private static readonly JsonSerializerOptions Options = new() {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Dictionary<MemberRole, string> DefaultRoleEmoji = new() {
        [MemberRole.Dps] = "⚔️",
        [MemberRole.Tank] = "🛡️",
        [MemberRole.Healer] = "💚"
    };

    private static readonly Dictionary<MemberTag, string> DefaultTagEmoji = new() {
        [MemberTag.Jungle] = "🌲",
        [MemberTag.Boss] = "👹",
        [MemberTag.Backup] = "🔁"
    };

    public FileEmojiSettingsStore(IHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "emoji-settings.json");
        (_roleEmoji, _tagEmoji) = Load();
    }

    private (Dictionary<MemberRole, string>, Dictionary<MemberTag, string>) Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return (new(DefaultRoleEmoji), new(DefaultTagEmoji));

            var json = File.ReadAllText(_filePath);
            var saved = JsonSerializer.Deserialize<EmojiSettingsDto>(json, Options);
            var roles = new Dictionary<MemberRole, string>(DefaultRoleEmoji);
            var tags = new Dictionary<MemberTag, string>(DefaultTagEmoji);

            if (saved is not null)
            {
                foreach (var r in saved.Roles)
                    if (!string.IsNullOrWhiteSpace(r.Emoji)) roles[r.Role] = r.Emoji.Trim();
                foreach (var t in saved.Tags)
                    if (!string.IsNullOrWhiteSpace(t.Emoji)) tags[t.Tag] = t.Emoji.Trim();
            }
            return (roles, tags);
        }
        catch
        {
            return (new(DefaultRoleEmoji), new(DefaultTagEmoji));
        }
    }

    private void Save()
    {
        lock (_fileLock)
        {
            try
            {
                var dto = Get();
                var json = JsonSerializer.Serialize(dto, Options);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
            }
        }
    }

    public EmojiSettingsDto Get() => new(
        Enum.GetValues<MemberRole>().Select(r => new RoleEmojiEntryDto(r, _roleEmoji.TryGetValue(r, out var e) ? e : DefaultRoleEmoji[r])).ToList(),
        Enum.GetValues<MemberTag>().Select(t => new TagEmojiEntryDto(t, _tagEmoji.TryGetValue(t, out var e) ? e : DefaultTagEmoji[t])).ToList()
    );

    public EmojiSettingsDto Update(EmojiSettingsDto dto)
    {
        foreach (var r in dto.Roles)
            _roleEmoji[r.Role] = string.IsNullOrWhiteSpace(r.Emoji) ? DefaultRoleEmoji[r.Role] : r.Emoji.Trim();
        foreach (var t in dto.Tags)
            _tagEmoji[t.Tag] = string.IsNullOrWhiteSpace(t.Emoji) ? DefaultTagEmoji[t.Tag] : t.Emoji.Trim();
        Save();
        return Get();
    }

    public string RoleEmoji(MemberRole role) => _roleEmoji.TryGetValue(role, out var e) ? e : DefaultRoleEmoji[role];
    public string TagEmoji(MemberTag tag) => _tagEmoji.TryGetValue(tag, out var e) ? e : DefaultTagEmoji[tag];
}

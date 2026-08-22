using GvGPoc.Hubs;
using GvGPoc.Models;
using GvGPoc.State;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{

    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
})
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });


builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<IBattleState, InMemoryBattleState>();
builder.Services.AddSingleton<IAccountStore, FileAccountStore>();


static byte[] GetOrCreateSessionSigningKey(IConfiguration config, string contentRootPath)
{
    var configured = config["SessionSigningKey"];
    if (!string.IsNullOrWhiteSpace(configured))
        return Convert.FromBase64String(configured);

    var keyFilePath = Path.Combine(contentRootPath, "session-signing.key");
    if (File.Exists(keyFilePath))
        return Convert.FromBase64String(File.ReadAllText(keyFilePath).Trim());

    var newKey = RandomNumberGenerator.GetBytes(32);
    File.WriteAllText(keyFilePath, Convert.ToBase64String(newKey));
    return newKey;
}

var sessionSigningKey = GetOrCreateSessionSigningKey(builder.Configuration, builder.Environment.ContentRootPath);
// Токен живёт 30 дней 
var sessionTtlDays = builder.Configuration.GetValue<int?>("SessionTtlDays") ?? 30;
builder.Services.AddSingleton(new SessionTokenOptions(sessionSigningKey, TimeSpan.FromDays(sessionTtlDays)));
builder.Services.AddSingleton<ISessionStore, SignedSessionStore>();

builder.Services.AddSingleton<IPendingDiscordStore, InMemoryPendingDiscordStore>();
builder.Services.AddSingleton<IMemberPresetStore, FileMemberPresetStore>();
builder.Services.AddSingleton<IDiscordWebhookStore, FileDiscordWebhookStore>();
builder.Services.AddSingleton<IEmojiSettingsStore, FileEmojiSettingsStore>();
builder.Services.AddSingleton<IConnectionTracker, InMemoryConnectionTracker>();
builder.Services.AddSingleton<HubActionRateLimiter>();
builder.Services.AddSingleton<LoginAttemptRateLimiter>();
builder.Services.AddHttpClient();

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

static string DiscordResultPage(bool success, string message)
{
    var color = success ? "#4caf50" : "#e33";
    return "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>Discord link</title>" +
           "<style>body{background:#111;color:#eee;font-family:system-ui,sans-serif;padding:60px 20px;text-align:center;}" +
           ".msg{color:" + color + ";}</style></head><body><p class=\"msg\">" +
           System.Net.WebUtility.HtmlEncode(message) + "</p>" +
           "<p style=\"color:#888;font-size:13px;\">Redirecting...</p>" +
           "<script>setTimeout(() => location.href = '/menu.html', 2000);</script></body></html>";
}

// Войти через Discord
static string DiscordLoginSuccessPage(string token, string username, UserRole role, string? squadId)
{
    var payload = JsonSerializer.Serialize(new { token, username, role = role.ToString(), squadId });
    var safeLiteral = payload.Replace("</", "<\\/"); 
    return "<!DOCTYPE html><html><head><meta charset=\"UTF-8\"><title>Discord login</title>" +
           "<style>body{background:#111;color:#eee;font-family:system-ui,sans-serif;padding:60px 20px;text-align:center;}</style></head><body>" +
           "<p style=\"color:#4caf50;\">Вход выполнен, переходим…</p>" +
           "<script>localStorage.setItem('gvg_session', JSON.stringify(" + safeLiteral + ")); location.href = '/menu.html';</script>" +
           "</body></html>";
}

// discordId, discordUsername
static async Task<(string discordId, string discordUsername, bool guildOk)?> ExchangeDiscordCode(
    string code, IConfiguration config, IHttpClientFactory httpClientFactory)
{
    var clientId = config["DiscordClientId"];
    var clientSecret = config["DiscordClientSecret"];
    var redirectUri = config["DiscordRedirectUri"];

    var http = httpClientFactory.CreateClient();

    var tokenResponse = await http.PostAsync("https://discord.com/api/oauth2/token", new FormUrlEncodedContent(new Dictionary<string, string> {
        ["client_id"] = clientId ?? "",
        ["client_secret"] = clientSecret ?? "",
        ["grant_type"] = "authorization_code",
        ["code"] = code,
        ["redirect_uri"] = redirectUri ?? ""
    }));

    if (!tokenResponse.IsSuccessStatusCode) return null;

    var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
    var accessToken = tokenJson.GetProperty("access_token").GetString();

    var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
    userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    var userResponse = await http.SendAsync(userRequest);
    if (!userResponse.IsSuccessStatusCode) return null;

    var userJson = await userResponse.Content.ReadFromJsonAsync<JsonElement>();
    var discordId = userJson.GetProperty("id").GetString()!;
    var discordUsername = userJson.GetProperty("username").GetString() ?? "unknown";

    var requiredGuildId = config["DiscordGuildId"];
    var guildOk = true;
    if (!string.IsNullOrEmpty(requiredGuildId))
    {
        guildOk = false;
        var guildsRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me/guilds");
        guildsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var guildsResponse = await http.SendAsync(guildsRequest);
        if (guildsResponse.IsSuccessStatusCode)
        {
            var guildsJson = await guildsResponse.Content.ReadFromJsonAsync<JsonElement>();
            foreach (var g in guildsJson.EnumerateArray())
            {
                if (g.GetProperty("id").GetString() == requiredGuildId) { guildOk = true; break; }
            }
        }
    }

    return (discordId, discordUsername, guildOk);
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/auth/login", (
    LoginDto req,
    HttpContext http,
    IAccountStore accounts,
    ISessionStore sessions,
    LoginAttemptRateLimiter loginLimiter) => {
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!loginLimiter.Allow(ip))
            return Results.Json(new AuthResponseDto(false, "Too many attempts, wait a bit.", null, null, null, null), statusCode: 429);

        var account = accounts.Find(req.Username);
        if (account is null || !accounts.VerifyPassword(account, req.Password))
            return Results.Json(new AuthResponseDto(false, "Неверный логин или пароль", null, null, null, null), statusCode: 401);

        var token = sessions.CreateSession(new SessionInfo(account.Username, account.Role, account.SquadId));
        return Results.Json(new AuthResponseDto(true, "Успешный вход", token, account.Username, account.Role, account.SquadId));
    });

app.MapPost("/api/auth/logout", (LogoutDto req, ISessionStore sessions) => {
    sessions.Remove(req.Token);
    return Results.Ok();
});

app.MapGet("/api/auth/discord/config", (IConfiguration config) =>
    Results.Ok(new DiscordConfigDto(config["DiscordClientId"] ?? "", config["DiscordRedirectUri"] ?? "")));

// войти через Discord (без пароля)
app.MapGet("/api/auth/discord/login-start", (ISessionStore sessions) => {
    var state = sessions.CreateSession(new SessionInfo("", UserRole.Player, null));
    return Results.Ok(new DiscordLoginStartDto(state));
});

app.MapGet("/api/auth/discord/callback", async (
    string? code,
    string? state,
    string? error,
    IConfiguration config,
    ISessionStore sessions,
    IAccountStore accounts,
    IPendingDiscordStore pending,
    IHttpClientFactory httpClientFactory) => {
        if (error is not null || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Results.Content(DiscordResultPage(false, "Discord отменил авторизацию."), "text/html");

        var session = sessions.Get(state);
        if (session is null)
            return Results.Content(DiscordResultPage(false, "Сессия истекла — попробуйте снова."), "text/html");

        var isLoginIntent = string.IsNullOrEmpty(session.Username);

        var exchanged = await ExchangeDiscordCode(code, config, httpClientFactory);
        if (exchanged is null)
            return Results.Content(DiscordResultPage(false, "Не удалось обменяться данными с Discord — попробуйте снова."), "text/html");

        var (discordId, discordUsername, guildOk) = exchanged.Value;
        if (!guildOk)
            return Results.Content(DiscordResultPage(false, "Этот Discord-аккаунт не состоит в сервере гильдии."), "text/html");

        if (isLoginIntent)
        {
            // Войти через Discord
            var existing = accounts.FindByDiscordId(discordId);
            if (existing is not null)
            {
                var loginToken = sessions.CreateSession(new SessionInfo(existing.Username, existing.Role, existing.SquadId));
                return Results.Content(DiscordLoginSuccessPage(loginToken, existing.Username, existing.Role, existing.SquadId), "text/html");
            }

            // Первый визит с Discord 
            var pendingToken = pending.Create(discordId, discordUsername);
            return Results.Redirect(
                "/discord-register.html?pendingToken=" + Uri.EscapeDataString(pendingToken) +
                "&discordUsername=" + Uri.EscapeDataString(discordUsername));
        }

        // Привязать Discord к уже существующему аккаунту
        var existingLink = accounts.FindByDiscordId(discordId);
        if (existingLink is not null && !string.Equals(existingLink.Username, session.Username, StringComparison.OrdinalIgnoreCase))
            return Results.Content(DiscordResultPage(false, "Этот Discord-аккаунт уже привязан к другому пользователю Tacnet."), "text/html");

        accounts.LinkDiscord(session.Username, discordId, discordUsername);
        return Results.Content(DiscordResultPage(true, $"Discord привязан: {discordUsername}"), "text/html");
    });

// регистрация
app.MapPost("/api/auth/discord/register", (
    DiscordRegisterDto req,
    IPendingDiscordStore pending,
    IAccountStore accounts,
    ISessionStore sessions) => {
        if (string.IsNullOrWhiteSpace(req.PendingToken))
            return Results.Json(new AuthResponseDto(false, "Сессия регистрации истекла — войдите через Discord заново.", null, null, null, null), statusCode: 400);

        var pendingReg = pending.Get(req.PendingToken);
        if (pendingReg is null)
            return Results.Json(new AuthResponseDto(false, "Сессия регистрации истекла — войдите через Discord заново.", null, null, null, null), statusCode: 400);

        // аккаунт для этого Discord уже появился.
        var already = accounts.FindByDiscordId(pendingReg.DiscordId);
        if (already is not null)
        {
            pending.Remove(req.PendingToken);
            var t = sessions.CreateSession(new SessionInfo(already.Username, already.Role, already.SquadId));
            return Results.Json(new AuthResponseDto(true, "Вход выполнен", t, already.Username, already.Role, already.SquadId));
        }

        var nickname = (req.Nickname ?? "").Trim();
        var validNick = nickname.Length is >= 2 and <= 24 &&
            nickname.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
        if (!validNick)
            return Results.Json(new AuthResponseDto(false, "Ник: 2-24 символа, только буквы/цифры/_/-.", null, null, null, null), statusCode: 400);

        if (accounts.Find(nickname) is not null)
            return Results.Json(new AuthResponseDto(false, "Такой ник уже занят в системе — выберите другой.", null, null, null, null), statusCode: 409);

        // случайный пароль, чтобы можно было войти через Discord без пароля
        var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        accounts.Upsert(nickname, UserRole.Player, null, randomPassword);
        accounts.LinkDiscord(nickname, pendingReg.DiscordId, pendingReg.DiscordUsername);
        pending.Remove(req.PendingToken);

        var token = sessions.CreateSession(new SessionInfo(nickname, UserRole.Player, null));
        return Results.Json(new AuthResponseDto(true, "Аккаунт создан", token, nickname, UserRole.Player, null));
    });

app.MapHub<BattleHub>("/battleHub");

app.Run();
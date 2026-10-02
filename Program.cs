using GvGPoc.Hubs;
using GvGPoc.Models;
using GvGPoc.State;
using GvGPoc.Endpoints;
using GvGPoc.Services;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.HttpOverrides;

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
builder.Services.AddSingleton<GvGPoc.Data.IDbConnectionFactory, GvGPoc.Data.NpgsqlConnectionFactory>();
builder.Services.AddSingleton<IAccountStore, PostgresAccountStore>();
builder.Services.AddSingleton<IDiscordAuthService, DiscordAuthService>();

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

var sessionTtlDays = builder.Configuration.GetValue<int?>("SessionTtlDays") ?? 30;
builder.Services.AddSingleton(new SessionTokenOptions(sessionSigningKey, TimeSpan.FromDays(sessionTtlDays)));
builder.Services.AddSingleton<ISessionStore, SignedSessionStore>();

builder.Services.AddSingleton<IPendingDiscordStore, InMemoryPendingDiscordStore>();
builder.Services.AddSingleton<IMemberPresetStore, FileMemberPresetStore>();
builder.Services.AddSingleton<IDiscordWebhookStore, FileDiscordWebhookStore>();
builder.Services.AddSingleton<IEmojiSettingsStore, FileEmojiSettingsStore>();
builder.Services.AddSingleton<IOrderMacroStore, FileOrderMacroStore>();
builder.Services.AddSingleton<IGoalOrderMacroStore, FileGoalOrderMacroStore>();
builder.Services.AddSingleton<IConnectionTracker, InMemoryConnectionTracker>();
builder.Services.AddSingleton<HubActionRateLimiter>();
builder.Services.AddSingleton<LoginAttemptRateLimiter>();
builder.Services.AddHttpClient();

builder.Services.AddHostedService<GvGPoc.Services.BattleTimerService>();

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true);
        }
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.Services.GetRequiredService<IAccountStore>();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions {
    OnPrepareResponse = context => {
        if (context.File.Name.EndsWith(".html"))
        {
            context.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            context.Context.Response.Headers.Pragma = "no-cache";
            context.Context.Response.Headers.Expires = "-1";
        }
    }
});

app.MapGroup("/api/auth").MapAuthEndpoints();
app.MapGroup("/api/auth/discord").MapDiscordEndpoints();
app.MapGroup("/api/replays").MapReplayEndpoints();

app.MapHub<BattleHub>("/battleHub");

app.Run();

public partial class Program { }

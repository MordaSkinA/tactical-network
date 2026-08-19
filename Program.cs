using GvGPoc.Hubs;
using GvGPoc.Models;
using GvGPoc.State;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    // true только при "dotnet run" локально (Development). На VPS обычно
    // ASPNETCORE_ENVIRONMENT=Production по умолчанию — там будет false,
    // сервер не покажет внутренние детали ошибок случайным подключениям.
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
})
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Тот же конвертер нужен и для обычных minimal API эндпоинтов (/api/auth/login),
// у SignalR — свой отдельный JsonSerializerOptions, он на Results.Json(...) не влияет.
// Без этого UserRole уходил на клиент как число (0, 1, 2...), а не как "Admin"/"Player" —
// из-за этого ломались проверки роли на фронте (index.html/auth.js сравнивают со строками).
builder.Services.ConfigureHttpJsonOptions(options => {
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<IBattleState, InMemoryBattleState>();
builder.Services.AddSingleton<IAccountStore, FileAccountStore>();
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<HubActionRateLimiter>();
builder.Services.AddSingleton<LoginAttemptRateLimiter>();

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

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

app.MapHub<BattleHub>("/battleHub");

app.Run();
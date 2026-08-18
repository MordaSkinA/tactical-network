using GvGPoc.Hubs;
using GvGPoc.Models;
using GvGPoc.State;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
    {
        // POC-only: показывать реальный текст исключения клиенту вместо общей фразы
        // "Failed to invoke ... due to an error on the server". Убрать перед тем,
        // как это станет чем-то большим, чем локальный тест — иначе можно случайно
        // раскрыть детали реализации.
        options.EnableDetailedErrors = true;
    })
    .AddJsonProtocol(options =>
    {
        // По умолчанию System.Text.Json сериализует enum'ы как числа, а фронтенд
        // отправляет строки ("Push", "TwinBlades" и т.д.) — без этого конвертера
        // ReportEvent/IssueOrder падают с ошибкой десериализации на сервере.
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSingleton<IBattleState, InMemoryBattleState>();

// POC-only: разрешаем любой origin, чтобы можно было открыть страницы
// с телефона/другого ПК через туннель (ngrok/Cloudflare Tunnel) без возни с CORS.
// Перед тем как это станет чем-то большим, чем POC, — сузить.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();   // отдаёт wwwroot/index.html, если зайти на "/"
app.UseStaticFiles();    // отдаёт wwwroot/observer.html и wwwroot/dashboard.html
app.MapHub<BattleHub>("/battleHub");

// --- REST API Эндпоинты для авторизации ---

app.MapPost("/api/auth/login", (LoginDto login, IBattleState state) => {
    var user = state.ValidateUser(login.Username, login.Password);
    if (user == null)
    {
        return Results.Json(
            new AuthResponseDto(false, "Неверный логин или пароль", null, null, null),
            statusCode: 401
        );
    }

    // В POC возвращаем данные пользователя. В полноценной версии (Phase 1) здесь будет выдаваться JWT-токен.
    return Results.Json(new AuthResponseDto(true, "Успешный вход", user.Username, user.Role, user.SquadId));
});

app.MapPost("/api/auth/create", (CreateUserDto dto, IBattleState state, IConfiguration config) => {
    var adminKey = config["AdminKey"] ?? "gvg-admin";
    if (dto.AdminKey != adminKey)
    {
        return Results.Unauthorized();
    }

    var newUser = new UserAccount {
        Username = dto.Username,
        PasswordHash = dto.Password, // В POC храним пароль как есть
        Role = dto.Role,
        SquadId = dto.SquadId
    };

    if (state.CreateUser(newUser))
    {
        return Results.Ok(new { success = true });
    }

    return Results.BadRequest(new { success = false, message = "Пользователь уже существует" });
});

app.Run();

using System.Text.Json.Serialization;
using GvGPoc.Hubs;
using GvGPoc.State;

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

app.Run();

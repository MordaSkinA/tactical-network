using GvGPoc.Hubs;
using GvGPoc.Models;
using GvGPoc.State;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = true;
    })
    .AddJsonProtocol(options =>
    {
 
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSingleton<IBattleState, InMemoryBattleState>();


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
app.UseDefaultFiles();   // отдаёт wwwroot/index.html
app.UseStaticFiles();    // отдаёт wwwroot/observer.html wwwroot/dashboard.html
app.MapHub<BattleHub>("/battleHub");

// REST API 

app.MapPost("/api/auth/login", (LoginDto login, IBattleState state) => {
    var user = state.ValidateUser(login.Username, login.Password);
    if (user == null)
    {
        return Results.Json(
            new AuthResponseDto(false, "Неверный логин или пароль", null, null, null),
            statusCode: 401
        );
    }

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
        PasswordHash = dto.Password, 
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

using System.Text.Json.Serialization;
using GvGPoc.Hubs;
using GvGPoc.State;

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
app.UseDefaultFiles(); 
app.UseStaticFiles();  
app.MapHub<BattleHub>("/battleHub");

app.Run();

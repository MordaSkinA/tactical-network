using GvGPoc.Extensions;
using GvGPoc.Models;
using GvGPoc.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;

namespace GvGPoc.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (
            LoginDto req,
            HttpContext http,
            IAccountStore accounts,
            ISessionStore sessions,
            LoginAttemptRateLimiter loginLimiter) => {

            var ip = http.GetRemoteIp();
            if (!loginLimiter.Allow(ip))
                return Results.Json(new AuthResponseDto(false, "Too many attempts, wait a bit.", null, null, null, null), statusCode: 429);

            var account = accounts.Find(req.Username);
            if (account is null || !accounts.VerifyPassword(account, req.Password))
                return Results.Json(new AuthResponseDto(false, "Invalid username or password", null, null, null, null), statusCode: 401);

            await accounts.RecordLoginAsync(account.Username, ip);
            var token = sessions.CreateSession(new SessionInfo(account.Username, account.Role, account.SquadId));
            return Results.Json(new AuthResponseDto(true, "Logged in successfully", token, account.Username, account.Role, account.SquadId));
        });

        app.MapPost("/logout", (LogoutDto req, ISessionStore sessions) => {
            sessions.Remove(req.Token);
            return Results.Ok();
        });
    }
}

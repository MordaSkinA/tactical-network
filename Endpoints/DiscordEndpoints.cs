namespace GvGPoc.Endpoints;

using GvGPoc.Extensions;
using GvGPoc.Models;
using GvGPoc.State;
using GvGPoc.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public static class DiscordEndpoints
{
    public static void MapDiscordEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/config", (IConfiguration config) =>
            Results.Ok(new DiscordConfigDto(config["DiscordClientId"] ?? "", config["DiscordRedirectUri"] ?? "")));

        app.MapGet("/login-start", (ISessionStore sessions) => {
            var state = sessions.CreateSession(new SessionInfo("", UserRole.Player, null));
            return Results.Ok(new DiscordLoginStartDto(state));
        });

        app.MapGet("/callback", async (
            string? code,
            string? state,
            string? error,
            HttpContext http,
            IConfiguration config,
            ISessionStore sessions,
            IAccountStore accounts,
            IPendingDiscordStore pending,
            IDiscordAuthService discordAuth,
            ILoggerFactory loggerFactory) => {

            var logger = loggerFactory.CreateLogger("DiscordEndpoints");
            var ip = http.GetRemoteIp();
            if (error is not null || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            {
                logger.LogWarning("Discord callback failed: error={Error}, code={Code}, state={State}. IP={IP}", error, code, state, ip);
                return Results.Content(ResponseTemplates.DiscordResultPage(false, "Discord canceled the authorization."), "text/html");
            }

            var session = sessions.Get(state);
            if (session is null)
            {
                logger.LogWarning("Discord callback session expired. State={State}, IP={IP}", state, ip);
                return Results.Content(ResponseTemplates.DiscordResultPage(false, "Session expired, please try again."), "text/html");
            }

            var isLoginIntent = string.IsNullOrEmpty(session.Username);

            try
            {
                var httpClientFactory = http.RequestServices.GetRequiredService<IHttpClientFactory>();
                var (discordId, discordUsername, guildOk, exchangeError) = await discordAuth.ExchangeCodeAsync(code, config, httpClientFactory);
                if (exchangeError is not null)
                {
                    logger.LogError("Discord exchange failed: {Error}. Code={Code}, IP={IP}", exchangeError, code, ip);
                    return Results.Content(ResponseTemplates.DiscordResultPage(false, $"Discord Error: {exchangeError}"), "text/html");
                }

                if (!guildOk)
                {
                    logger.LogInformation("Discord user {DiscordId} is not in the required guild. IP={IP}", discordId, ip);
                    return Results.Content(ResponseTemplates.DiscordResultPage(false, "This Discord account isn't a member of the guild server."), "text/html");
                }

                if (isLoginIntent)
                {
                    var existing = accounts.FindByDiscordId(discordId);
                    if (existing is not null)
                    {
                        await accounts.RecordLoginAsync(existing.Username, ip);
                        var loginToken = sessions.CreateSession(new SessionInfo(existing.Username, existing.Role, existing.SquadId));

                        var payload = new { token = loginToken, username = existing.Username, role = existing.Role.ToString(), squadId = existing.SquadId };
                        return Results.Content(ResponseTemplates.DiscordLoginSuccessPage(JsonSerializer.Serialize(payload)), "text/html");
                    }

                    var pendingToken = pending.Create(discordId, discordUsername);
                    return Results.Redirect(
                        "/discord-register.html?pendingToken=" + Uri.EscapeDataString(pendingToken) +
                        "&discordUsername=" + Uri.EscapeDataString(discordUsername));
                }

                var existingLink = accounts.FindByDiscordId(discordId);
                if (existingLink is not null && !string.Equals(existingLink.Username, session.Username, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Discord account {DiscordId} already linked to {OtherUser}. Attempt by {User}. IP={IP}", discordId, existingLink.Username, session.Username, ip);
                    return Results.Content(ResponseTemplates.DiscordResultPage(false, "This Discord account is already linked to another Tacnet user."), "text/html");
                }

                await accounts.LinkDiscordAsync(session.Username, discordId, discordUsername);
                return Results.Content(ResponseTemplates.DiscordResultPage(true, $"Discord linked: {discordUsername}"), "text/html");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during Discord callback for code={Code}. IP={IP}", code, ip);
                return Results.Content(ResponseTemplates.DiscordResultPage(false, "An internal error occurred. Please try again."), "text/html");
            }
        });

        app.MapPost("/register", async (
            DiscordRegisterDto req,
            HttpContext http,
            IPendingDiscordStore pending,
            IAccountStore accounts,
            ISessionStore sessions,
            ILoggerFactory loggerFactory) => {

            var logger = loggerFactory.CreateLogger("DiscordEndpoints");
            var ip = http.GetRemoteIp();
            if (string.IsNullOrWhiteSpace(req.PendingToken))
            {
                logger.LogWarning("Discord register attempt with missing token. IP={IP}", ip);
                return Results.Json(new AuthResponseDto(false, "Registration session expired, log in via Discord again.", null, null, null, null), statusCode: 400);
            }

            var pendingReg = pending.Get(req.PendingToken);
            if (pendingReg is null)
            {
                logger.LogWarning("Discord register attempt with invalid token {Token}. IP={IP}", req.PendingToken, ip);
                return Results.Json(new AuthResponseDto(false, "Registration session expired, log in via Discord again.", null, null, null, null), statusCode: 400);
            }

            var already = accounts.FindByDiscordId(pendingReg.DiscordId);
            if (already is not null)
            {
                pending.Remove(req.PendingToken);
                await accounts.RecordLoginAsync(already.Username, ip);
                var t = sessions.CreateSession(new SessionInfo(already.Username, already.Role, already.SquadId));
                logger.LogInformation("Discord register: user {Username} already exists, logging them in. IP={IP}", already.Username, ip);
                return Results.Json(new AuthResponseDto(true, "Logged in", t, already.Username, already.Role, already.SquadId));
            }

            var nickname = (req.Nickname ?? "").Trim();
            var validNick = nickname.Length is >= 2 and <= 24 &&
                nickname.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
            if (!validNick)
            {
                logger.LogWarning("Discord register: invalid nickname {Nickname}. IP={IP}", nickname, ip);
                return Results.Json(new AuthResponseDto(false, "Nickname: 2-24 characters, letters/digits/_/- only.", null, null, null, null), statusCode: 400);
            }

            if (accounts.Find(nickname) is not null)
            {
                logger.LogWarning("Discord register: nickname {Nickname} already taken. IP={IP}", nickname, ip);
                return Results.Json(new AuthResponseDto(false, "That nickname is already taken, pick another one.", null, null, null, null), statusCode: 409);
            }

            var randomPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
            await accounts.UpsertAsync(nickname, UserRole.Player, null, randomPassword);
            await accounts.LinkDiscordAsync(nickname, pendingReg.DiscordId, pendingReg.DiscordUsername);
            pending.Remove(req.PendingToken);
            await accounts.RecordLoginAsync(nickname, ip);

            var token = sessions.CreateSession(new SessionInfo(nickname, UserRole.Player, null));
            logger.LogInformation("Discord register: created account {Username} linked to {DiscordId}. IP={IP}", nickname, pendingReg.DiscordId, ip);
            return Results.Json(new AuthResponseDto(true, "Account created", token, nickname, UserRole.Player, null));
        });
    }
}

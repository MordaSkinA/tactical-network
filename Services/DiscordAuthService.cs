using GvGPoc.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GvGPoc.Services;

public interface IDiscordAuthService
{
    Task<(string discordId, string discordUsername, bool guildOk, string? error)> ExchangeCodeAsync(string code, IConfiguration config, IHttpClientFactory httpClientFactory);
}

public class DiscordAuthService : IDiscordAuthService
{
    public async Task<(string discordId, string discordUsername, bool guildOk, string? error)> ExchangeCodeAsync(string code, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        var clientId = config["DiscordClientId"];
        var clientSecret = config["DiscordClientSecret"];
        var redirectUri = config["DiscordRedirectUri"];

        var http = httpClientFactory.CreateClient();

        try
        {
            var tokenResponse = await http.PostAsync("https://discord.com/api/oauth2/token", new FormUrlEncodedContent(new Dictionary<string, string> {
                ["client_id"] = clientId ?? "",
                ["client_secret"] = clientSecret ?? "",
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri ?? ""
            }));

            if (!tokenResponse.IsSuccessStatusCode)
                return (null!, null!, false, $"Token exchange failed with status {tokenResponse.StatusCode}");

            var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenJson.GetProperty("access_token").GetString();

            var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userResponse = await http.SendAsync(userRequest);
            if (!userResponse.IsSuccessStatusCode)
                return (null!, null!, false, $"User info request failed with status {userResponse.StatusCode}");

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

            return (discordId, discordUsername, guildOk, null);
        }
        catch (Exception ex)
        {
            return (null!, null!, false, ex.Message);
        }
    }
}

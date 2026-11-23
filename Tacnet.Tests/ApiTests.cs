using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GvGPoc.Models;
using GvGPoc.State;
using GvGPoc.Data;
using GvGPoc.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Tacnet.Tests;

public class ApiTests : IClassFixture<ApiTests.CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ApiTests.CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiTests(ApiTests.CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Reset mocks for every test
        _factory.AccountStoreMock.Reset();
        _factory.DbConnectionFactoryMock.Reset();
        _factory.DiscordAuthServiceMock.Reset();
        _factory.PendingDiscordStoreMock.Reset();
        _factory.SessionStoreMock.Reset();
        _factory.BattleStateMock.Reset();
    }

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Mock<IAccountStore> AccountStoreMock { get; } = new();
        public Mock<IDbConnectionFactory> DbConnectionFactoryMock { get; } = new();
        public Mock<IDiscordAuthService> DiscordAuthServiceMock { get; } = new();
        public Mock<IPendingDiscordStore> PendingDiscordStoreMock { get; } = new();
        public Mock<ISessionStore> SessionStoreMock { get; } = new();
        public Mock<IBattleState> BattleStateMock { get; } = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "ConnectionStrings:Tacnet", "Host=localhost;Database=tacnet;Username=postgres;Password=postgres" },
                        { "SessionSigningKey", "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=" },
                        { "TestKey", "test_id" },
                        { "DiscordClientId", "test_id" },
                        { "DiscordClientSecret", "test_secret" },
                        { "DiscordRedirectUri", "http://localhost/callback" },
                        { "DiscordGuildId", "test_guild" }
                    })
                    .Build();
                services.AddSingleton<IConfiguration>(configuration);

                services.AddSingleton<IAccountStore>(AccountStoreMock.Object);
                services.AddSingleton<IDbConnectionFactory>(DbConnectionFactoryMock.Object);
                services.AddSingleton<IDiscordAuthService>(DiscordAuthServiceMock.Object);
                services.AddSingleton<IPendingDiscordStore>(PendingDiscordStoreMock.Object);
                services.AddSingleton<ISessionStore>(SessionStoreMock.Object);
                services.AddSingleton<IBattleState>(BattleStateMock.Object);
            });
        }
    }

    [Fact]
    public async Task GetDiscordConfig_ReturnsSuccessAndValidDto()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/discord/config");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var config = await response.Content.ReadFromJsonAsync<DiscordConfigDto>(JsonOptions);
        Assert.NotNull(config);
        Assert.Equal("test_id", config.ClientId);
        Assert.Equal("http://localhost/callback", config.RedirectUri);
    }

    [Fact]
    public async Task LoginStart_ReturnsToken()
    {
        _factory.SessionStoreMock.Setup(s => s.CreateSession(It.IsAny<SessionInfo>())).Returns("state_token");
        // Act
        var response = await _client.GetAsync("/api/auth/discord/login-start");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<DiscordLoginStartDto>(JsonOptions);
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrEmpty(dto.State));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto("non_existent_user", "wrong_password");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DiscordCallback_Success_LinksAccount()
    {
        // Arrange
        var code = "test_code";
        var state = "test_state";
        var discordId = "discord_123";
        var discordUsername = "test_user";
        var username = "existing_user";
        var session = new SessionInfo(username, UserRole.Player, null);

        _factory.SessionStoreMock.Setup(s => s.Get(state)).Returns(session);
        _factory.DiscordAuthServiceMock.Setup(d => d.ExchangeCodeAsync(code, It.IsAny<IConfiguration>(), It.IsAny<IHttpClientFactory>()))
            .ReturnsAsync((discordId, discordUsername, true, null));
        _factory.AccountStoreMock.Setup(a => a.FindByDiscordId(discordId)).Returns(new UserAccount { Username = username, Role = UserRole.Player });

        // Act
        var response = await _client.GetAsync($"/api/auth/discord/callback?code={code}&state={state}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _factory.AccountStoreMock.Verify(a => a.LinkDiscordAsync(username, discordId, discordUsername), Times.Once);
    }

    [Fact]
    public async Task DiscordRegister_Success_CreatesAccount()
    {
        // Arrange
        var token = "pending_token";
        var nickname = "new_user";
        var discordId = "discord_123";
        var discordUsername = "test_user";
        var pending = new PendingDiscordRegistration(discordId, discordUsername, DateTimeOffset.UtcNow.AddMinutes(10));

        _factory.PendingDiscordStoreMock.Setup(p => p.Get(token)).Returns(pending);
        _factory.AccountStoreMock.Setup(a => a.Find(nickname)).Returns((UserAccount?)null);
        _factory.AccountStoreMock.Setup(a => a.FindByDiscordId(discordId)).Returns((UserAccount?)null);
        _factory.SessionStoreMock.Setup(s => s.CreateSession(It.IsAny<SessionInfo>())).Returns("new_token");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/discord/register", new DiscordRegisterDto(token, nickname));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        Assert.True(authResponse!.Success);
        Assert.Equal(nickname, authResponse.Username);
        _factory.AccountStoreMock.Verify(a => a.UpsertAsync(nickname, UserRole.Player, null, It.IsAny<string>()), Times.Once);
        _factory.PendingDiscordStoreMock.Verify(p => p.Remove(token), Times.Once);
    }

    [Fact]
    public async Task ReplayDownload_Authorized_ReturnsFile()
    {
        // Arrange
        var fileName = "battle-2023.json";
        var accessToken = "valid_token";
        var session = new SessionInfo("commander_user", UserRole.Commander, null);
        var fileBytes = new byte[] { 1, 2, 3, 4 };

        _factory.SessionStoreMock.Setup(s => s.Get(accessToken)).Returns(session);
        _factory.BattleStateMock.Setup(b => b.ReadLogFileBytesAsync(fileName)).ReturnsAsync(fileBytes);

        // Act
        var response = await _client.GetAsync($"/api/replays/{fileName}/download?access_token={accessToken}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(fileBytes, content);
    }

    [Fact]
    public async Task ReplayDownload_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var fileName = "battle-2023.json";
        var accessToken = "invalid_token";

        _factory.SessionStoreMock.Setup(s => s.Get(accessToken)).Returns((SessionInfo?)null);

        // Act
        var response = await _client.GetAsync($"/api/replays/{fileName}/download?access_token={accessToken}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReplayDownload_Forbidden_ReturnsForbidden()
    {
        // Arrange
        var fileName = "battle-2023.json";
        var accessToken = "player_token";
        var session = new SessionInfo("player_user", UserRole.Player, null);

        _factory.SessionStoreMock.Setup(s => s.Get(accessToken)).Returns(session);

        // Act
        var response = await _client.GetAsync($"/api/replays/{fileName}/download?access_token={accessToken}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

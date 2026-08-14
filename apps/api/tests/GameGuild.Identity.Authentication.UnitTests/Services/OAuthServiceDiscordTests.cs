using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class OAuthServiceDiscordTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<OAuthService>> _loggerMock;
    private readonly OAuthService _oauthService;

    public OAuthServiceDiscordTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<OAuthService>>();

        _oauthService = new OAuthService(_httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithDiscord_ReturnsCorrectUrl()
    {
        // Arrange
        var clientId = "test-discord-client-id";
        _configurationMock.Setup(x => x["OAuth:discord:ClientId"]).Returns(clientId);
        var redirectUri = "https://example.com/callback";
        var state = "test-state";

        // Act
        var url = await _oauthService.GetAuthorizationUrlAsync("discord", redirectUri, state);

        // Assert
        url.Should().Contain("https://discord.com/oauth2/authorize");
        url.Should().Contain($"client_id={clientId}");
        url.Should().Contain($"redirect_uri={Uri.EscapeDataString(redirectUri)}");
        url.Should().Contain($"state={state}");
        url.Should().Contain($"scope={Uri.EscapeDataString("identify email")}");
        url.Should().Contain("response_type=code");
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithDiscordAndMissingClientId_ThrowsException()
    {
        // Arrange
        _configurationMock.Setup(x => x["OAuth:discord:ClientId"]).Returns((string?)null);

        // Act & Assert
        await _oauthService
            .Invoking(x => x.GetAuthorizationUrlAsync("discord", "https://example.com", "state"))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*OAuth client ID not configured*");
    }

    [Fact]
    public async Task HandleCallbackAsync_WithDiscord_ExchangesCodeAsFormUrlEncoded()
    {
        // Arrange
        _configurationMock.Setup(x => x["OAuth:Discord:ClientId"]).Returns("test-discord-client-id");
        _configurationMock.Setup(x => x["OAuth:Discord:ClientSecret"]).Returns("test-discord-client-secret");

        var requests = new List<CapturedRequest>();
        SetupHttpHandler(requests, tokenResponse: """{"access_token":"discord-access-token","token_type":"Bearer"}""");

        var redirectUri = "https://example.com/callback";

        // Act
        var profile = await _oauthService.HandleCallbackAsync("discord", "auth-code-123", "state-123", redirectUri);

        // Assert
        profile.Should().NotBeNull();

        requests.Should().HaveCount(2);
        var tokenRequest = requests[0];
        tokenRequest.Request.Method.Should().Be(HttpMethod.Post);
        tokenRequest.Request.RequestUri.Should().Be(new Uri("https://discord.com/api/oauth2/token"));
        tokenRequest.ContentType.Should().Be("application/x-www-form-urlencoded");

        tokenRequest.Body.Should().Contain("client_id=test-discord-client-id");
        tokenRequest.Body.Should().Contain("client_secret=test-discord-client-secret");
        tokenRequest.Body.Should().Contain("grant_type=authorization_code");
        tokenRequest.Body.Should().Contain("code=auth-code-123");
        tokenRequest.Body.Should().Contain($"redirect_uri={Uri.EscapeDataString(redirectUri)}");
    }

    [Fact]
    public async Task HandleCallbackAsync_WithDiscordAndMissingClientId_ThrowsException()
    {
        // Arrange
        _configurationMock.Setup(x => x["OAuth:Discord:ClientId"]).Returns((string?)null);
        _configurationMock.Setup(x => x["OAuth:Discord:ClientSecret"]).Returns((string?)null);

        var requests = new List<CapturedRequest>();
        SetupHttpHandler(requests, tokenResponse: """{"access_token":"discord-access-token"}""");

        // Act & Assert
        await _oauthService
            .Invoking(x => x.HandleCallbackAsync("discord", "auth-code", "state", "https://example.com/callback"))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Discord OAuth client ID*");
    }

    [Fact]
    public async Task HandleCallbackAsync_WithDiscordTokenError_ThrowsException()
    {
        // Arrange
        _configurationMock.Setup(x => x["OAuth:Discord:ClientId"]).Returns("test-discord-client-id");
        _configurationMock.Setup(x => x["OAuth:Discord:ClientSecret"]).Returns("test-discord-client-secret");

        var requests = new List<CapturedRequest>();
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                requests.Add(CaptureRequest(request));
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("""{"error":"invalid_grant","error_description":"Invalid OAuth2 grant_code"}""", Encoding.UTF8, "application/json")
                };
            });

        // Act & Assert
        await _oauthService
            .Invoking(x => x.HandleCallbackAsync("discord", "bad-code", "state", "https://example.com/callback"))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetUserProfileAsync_WithDiscord_MapsFullProfile()
    {
        // Arrange
        var accessToken = "discord-access-token";
        var requests = new List<CapturedRequest>();
        SetupHttpHandler(requests, userResponse: """{"id":"123456789012345678","username":"testuser","global_name":"Test User","email":"test@example.com","verified":true,"avatar":"abc123def456"}""");

        // Act
        var profile = await _oauthService.GetUserProfileAsync("discord", accessToken);

        // Assert
        requests.Should().HaveCount(1);
        var userRequest = requests[0];
        userRequest.Request.Method.Should().Be(HttpMethod.Get);
        userRequest.Request.RequestUri.Should().Be(new Uri("https://discord.com/api/v10/users/@me"));
        userRequest.Request.Headers.Authorization.Should().NotBeNull();
        userRequest.Request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        userRequest.Request.Headers.Authorization.Parameter.Should().Be(accessToken);

        profile.ProviderId.Should().Be("123456789012345678");
        profile.Provider.Should().Be("Discord");
        profile.Email.Should().Be("test@example.com");
        profile.EmailVerified.Should().BeTrue();
        profile.Name.Should().Be("Test User");
        profile.Username.Should().Be("testuser");
        profile.AvatarUrl.Should().Be("https://cdn.discordapp.com/avatars/123456789012345678/abc123def456.png?size=256");
        profile.AccessToken.Should().Be(accessToken);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithDiscordAnimatedAvatar_ReturnsGifUrl_AndFallsBackToUsername()
    {
        // Arrange
        var requests = new List<CapturedRequest>();
        SetupHttpHandler(requests, userResponse: """{"id":"123456789012345678","username":"testuser","global_name":null,"email":"test@example.com","verified":false,"avatar":"a_animatedhash"}""");

        // Act
        var profile = await _oauthService.GetUserProfileAsync("discord", "discord-access-token");

        // Assert
        profile.Name.Should().Be("testuser");
        profile.EmailVerified.Should().BeFalse();
        profile.AvatarUrl.Should().Be("https://cdn.discordapp.com/avatars/123456789012345678/a_animatedhash.gif?size=256");
    }

    [Fact]
    public async Task GetUserProfileAsync_WithDiscordNullAvatar_ReturnsDefaultEmbedAvatar()
    {
        // Arrange
        // Snowflake 25165824 == 6 << 22, so (id >> 22) % 6 == 0 → default avatar index 0.
        var requests = new List<CapturedRequest>();
        SetupHttpHandler(requests, userResponse: """{"id":"25165824","username":"testuser","global_name":null,"email":null,"verified":false,"avatar":null}""");

        // Act
        var profile = await _oauthService.GetUserProfileAsync("discord", "discord-access-token");

        // Assert
        profile.AvatarUrl.Should().Be("https://cdn.discordapp.com/embed/avatars/0.png");
    }

    /// <summary>
    ///     Snapshot of an outgoing HTTP request. Body and content type are read eagerly
    ///     because OAuthService disposes request content (via `using`) after sending.
    /// </summary>
    private sealed record CapturedRequest(HttpRequestMessage Request, string Body, string? ContentType);

    private static CapturedRequest CaptureRequest(HttpRequestMessage request)
    {
        var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
        var contentType = request.Content?.Headers.ContentType?.MediaType;

        return new CapturedRequest(request, body, contentType);
    }

    /// <summary>
    ///     Mocks the HTTP handler, recording requests and answering the token endpoint
    ///     and the user endpoint based on the request path.
    /// </summary>
    private void SetupHttpHandler(List<CapturedRequest> requests, string? tokenResponse = null, string? userResponse = null)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                requests.Add(CaptureRequest(request));

                var isTokenRequest = request.RequestUri!.AbsolutePath.EndsWith("/token", StringComparison.Ordinal);

                var payload = isTokenRequest
                    ? tokenResponse ?? """{"access_token":"discord-access-token"}"""
                    : userResponse ?? """{"id":"123456789012345678","username":"testuser","global_name":"Test User","email":"test@example.com","verified":true,"avatar":"abc123def456"}""";

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
            });
    }
}

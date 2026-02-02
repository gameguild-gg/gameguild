using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class OAuthServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<OAuthService>> _loggerMock;
    private readonly OAuthService _oauthService;

    public OAuthServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<OAuthService>>();
        
        _oauthService = new OAuthService(_httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithGitHub_ReturnsCorrectUrl()
    {
        // Arrange
        var clientId = "test-github-client-id";
        _configurationMock.Setup(x => x["OAuth:github:ClientId"]).Returns(clientId);
        var redirectUri = "https://example.com/callback";
        var state = "test-state";

        // Act
        var url = await _oauthService.GetAuthorizationUrlAsync("github", redirectUri, state);

        // Assert
        url.Should().Contain("https://github.com/login/oauth/authorize");
        url.Should().Contain($"client_id={clientId}");
        url.Should().Contain($"redirect_uri={Uri.EscapeDataString(redirectUri)}");
        url.Should().Contain($"state={state}");
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithGoogle_ReturnsCorrectUrl()
    {
        // Arrange
        var clientId = "test-google-client-id";
        _configurationMock.Setup(x => x["OAuth:google:ClientId"]).Returns(clientId);
        var redirectUri = "https://example.com/callback";
        var state = "test-state";

        // Act
        var url = await _oauthService.GetAuthorizationUrlAsync("google", redirectUri, state);

        // Assert
        url.Should().Contain("https://accounts.google.com/o/oauth2/v2/auth");
        url.Should().Contain($"client_id={clientId}");
        url.Should().Contain($"redirect_uri={Uri.EscapeDataString(redirectUri)}");
        url.Should().Contain($"state={state}");
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithMissingClientId_ThrowsException()
    {
        // Arrange
        _configurationMock.Setup(x => x["OAuth:github:ClientId"]).Returns((string?)null);

        // Act & Assert
        await _oauthService
            .Invoking(x => x.GetAuthorizationUrlAsync("github", "https://example.com", "state"))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*OAuth client ID not configured*");
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithUnsupportedProvider_ThrowsException()
    {
        // Arrange
        _configurationMock.Setup(x => x["OAuth:unsupported:ClientId"]).Returns("test-id");

        // Act & Assert
        await _oauthService
            .Invoking(x => x.GetAuthorizationUrlAsync("unsupported", "https://example.com", "state"))
            .Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("*OAuth provider not supported*");
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_WithScopes_IncludesScopesInUrl()
    {
        // Arrange
        var clientId = "test-github-client-id";
        _configurationMock.Setup(x => x["OAuth:github:ClientId"]).Returns(clientId);
        var scopes = new[] { "user:email", "read:user" };

        // Act
        var url = await _oauthService.GetAuthorizationUrlAsync("github", "https://example.com", "state", scopes);

        // Assert
        url.Should().Contain("scope=");
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidProvider_ReturnsTrue()
    {
        // Act
        var result = await _oauthService.RevokeTokenAsync("github", "test-token");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserProfileAsync_WithUnsupportedProvider_ThrowsException()
    {
        // Act & Assert
        await _oauthService
            .Invoking(x => x.GetUserProfileAsync("unsupported", "test-token"))
            .Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("*Provider not supported*");
    }
}

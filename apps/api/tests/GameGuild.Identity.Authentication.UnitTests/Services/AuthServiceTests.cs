using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authentication;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

/// <summary>
/// Tests for the AuthService facade that delegates to focused sub-services.
/// Since AuthService is a thin delegator, these tests verify correct delegation.
/// Detailed behavior testing belongs on LocalAuthService, OAuthAuthService, etc.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<ILocalAuthService> _localAuthServiceMock;
    private readonly Mock<IOAuthAuthService> _oauthAuthServiceMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IWeb3AuthService> _web3AuthServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _localAuthServiceMock = new Mock<ILocalAuthService>();
        _oauthAuthServiceMock = new Mock<IOAuthAuthService>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _web3AuthServiceMock = new Mock<IWeb3AuthService>();

        _authService = new AuthService(
            _localAuthServiceMock.Object,
            _oauthAuthServiceMock.Object,
            _passwordServiceMock.Object,
            _web3AuthServiceMock.Object
        );
    }

    // ── Local Auth Delegation ─────────────────────────────────

    [Fact]
    public async Task LocalSignInAsync_ShouldDelegateToLocalAuthService()
    {
        // Arrange
        var request = new LocalSignInRequest { Email = "test@example.com", Password = "Password123!" };
        var expectedResponse = new SignInResponse
        {
            Success = true,
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            UserId = Guid.NewGuid(),
            Email = "test@example.com"
        };

        _localAuthServiceMock
            .Setup(x => x.LocalSignInAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.LocalSignInAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        _localAuthServiceMock.Verify(x => x.LocalSignInAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LocalSignInAsync_WhenSubServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var request = new LocalSignInRequest { Email = "bad@example.com", Password = "wrong" };

        _localAuthServiceMock
            .Setup(x => x.LocalSignInAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LocalSignInAsync(request));
    }

    [Fact]
    public async Task LocalSignUpAsync_ShouldDelegateToLocalAuthService()
    {
        // Arrange
        var request = new LocalSignUpRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            Username = "newuser"
        };
        var expectedResponse = new SignInResponse
        {
            Success = true,
            AccessToken = "token",
            RefreshToken = "refresh",
            UserId = Guid.NewGuid(),
            Email = "new@example.com"
        };

        _localAuthServiceMock
            .Setup(x => x.LocalSignUpAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.LocalSignUpAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        _localAuthServiceMock.Verify(x => x.LocalSignUpAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LocalSignUpAsync_WhenSubServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var request = new LocalSignUpRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            Username = "existinguser"
        };

        _localAuthServiceMock
            .Setup(x => x.LocalSignUpAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Email already in use"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LocalSignUpAsync(request));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldDelegateToLocalAuthService()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "valid-refresh-token" };
        var expectedResponse = new SignInResponse
        {
            Success = true,
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            UserId = Guid.NewGuid()
        };

        _localAuthServiceMock
            .Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        _localAuthServiceMock.Verify(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenExpired_ShouldPropagateException()
    {
        // Arrange
        var request = new RefreshTokenRequest { RefreshToken = "expired-token" };

        _localAuthServiceMock
            .Setup(x => x.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Token expired"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.RefreshTokenAsync(request));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ShouldDelegateToLocalAuthService()
    {
        // Arrange
        var token = "token-to-revoke";
        var ipAddress = "192.168.1.1";

        _localAuthServiceMock
            .Setup(x => x.RevokeRefreshTokenAsync(token, ipAddress, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _authService.RevokeRefreshTokenAsync(token, ipAddress);

        // Assert
        _localAuthServiceMock.Verify(
            x => x.RevokeRefreshTokenAsync(token, ipAddress, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── OAuth Delegation ──────────────────────────────────────

    [Fact]
    public async Task GitHubSignInAsync_ShouldDelegateToOAuthAuthService()
    {
        // Arrange
        var request = new OAuthSignInRequest { Code = "github-code" };
        var expectedResponse = new SignInResponse { Success = true, AccessToken = "gh-token" };

        _oauthAuthServiceMock
            .Setup(x => x.GitHubSignInAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.GitHubSignInAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        _oauthAuthServiceMock.Verify(x => x.GitHubSignInAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoogleSignInAsync_ShouldDelegateToOAuthAuthService()
    {
        // Arrange
        var request = new OAuthSignInRequest { Code = "google-code" };
        var expectedResponse = new SignInResponse { Success = true, AccessToken = "goog-token" };

        _oauthAuthServiceMock
            .Setup(x => x.GoogleSignInAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.GoogleSignInAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        _oauthAuthServiceMock.Verify(x => x.GoogleSignInAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoogleIdTokenSignInAsync_ShouldDelegateToOAuthAuthService()
    {
        // Arrange
        var request = new GoogleIdTokenRequest { IdToken = "google-id-token" };
        var expectedResponse = new SignInResponse { Success = true };

        _oauthAuthServiceMock
            .Setup(x => x.GoogleIdTokenSignInAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.GoogleIdTokenSignInAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task DiscordSignInAsync_ShouldDelegateToOAuthAuthService()
    {
        var request = new DiscordSignInRequest { Code = "discord-code" };
        var expectedResponse = new SignInResponse { Success = true, AccessToken = "discord-token" };
        _oauthAuthServiceMock
            .Setup(instance => instance.DiscordSignInAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var result = await _authService.DiscordSignInAsync(request);

        result.Should().BeSameAs(expectedResponse);
        _oauthAuthServiceMock.Verify(
            instance => instance.DiscordSignInAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetGitHubAuthUrlAsync_ShouldDelegateToOAuthAuthService()
    {
        // Arrange
        var redirectUri = "https://example.com/callback";
        var expectedUrl = "https://github.com/login/oauth/authorize?client_id=xxx";

        _oauthAuthServiceMock
            .Setup(x => x.GetGitHubAuthUrlAsync(redirectUri))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _authService.GetGitHubAuthUrlAsync(redirectUri);

        // Assert
        result.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task GetGoogleAuthUrlAsync_ShouldDelegateToOAuthAuthService()
    {
        // Arrange
        var redirectUri = "https://example.com/callback";
        var expectedUrl = "https://accounts.google.com/o/oauth2/auth?client_id=yyy";

        _oauthAuthServiceMock
            .Setup(x => x.GetGoogleAuthUrlAsync(redirectUri))
            .ReturnsAsync(expectedUrl);

        // Act
        var result = await _authService.GetGoogleAuthUrlAsync(redirectUri);

        // Assert
        result.Should().Be(expectedUrl);
    }

    // ── Password Delegation ───────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_ShouldDelegateToPasswordService()
    {
        // Arrange
        var request = new PasswordResetRequest { Email = "test@example.com" };
        var expectedResponse = new EmailOperationResponse { Success = true, Message = "Reset email sent" };

        _passwordServiceMock
            .Setup(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        _passwordServiceMock.Verify(x => x.ForgotPasswordAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldDelegateToPasswordService()
    {
        // Arrange
        var request = new ResetPasswordRequest { Token = "reset-token", NewPassword = "NewPassword123!" };
        var expectedResponse = new EmailOperationResponse { Success = true, Message = "Password reset" };

        _passwordServiceMock
            .Setup(x => x.ResetPasswordAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.ResetPasswordAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldDelegateToPasswordService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new ChangePasswordRequest { CurrentPassword = "Old123!", NewPassword = "New123!" };
        var expectedResponse = new EmailOperationResponse { Success = true, Message = "Changed" };

        _passwordServiceMock
            .Setup(x => x.ChangePasswordAsync(request, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.ChangePasswordAsync(request, userId);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task SendEmailVerificationAsync_ShouldDelegateToPasswordService()
    {
        // Arrange
        var request = new SendEmailVerificationRequest { Email = "verify@example.com" };
        var expectedResponse = new EmailOperationResponse { Success = true };

        _passwordServiceMock
            .Setup(x => x.SendEmailVerificationAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.SendEmailVerificationAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task VerifyEmailAsync_ShouldDelegateToPasswordService()
    {
        // Arrange
        var request = new EmailVerificationRequest { Token = "verify-token" };
        var expectedResponse = new EmailOperationResponse { Success = true };

        _passwordServiceMock
            .Setup(x => x.VerifyEmailAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.VerifyEmailAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    // ── Web3 Delegation ───────────────────────────────────────

    [Fact]
    public async Task GenerateWeb3ChallengeAsync_ShouldDelegateToWeb3AuthService()
    {
        // Arrange
        var request = new Web3ChallengeRequest { WalletAddress = "0x1234567890abcdef" };
        var expectedResponse = new Web3ChallengeResponse { Challenge = "Sign this message" };

        _web3AuthServiceMock
            .Setup(x => x.GenerateWeb3ChallengeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.GenerateWeb3ChallengeAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        _web3AuthServiceMock.Verify(
            x => x.GenerateWeb3ChallengeAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyWeb3SignatureAsync_ShouldDelegateToWeb3AuthService()
    {
        // Arrange
        var request = new Web3VerificationRequest
        {
            WalletAddress = "0x1234567890abcdef",
            Signature = "0xsignature"
        };
        var expectedResponse = new SignInResponse { Success = true, AccessToken = "web3-token" };

        _web3AuthServiceMock
            .Setup(x => x.VerifyWeb3SignatureAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authService.VerifyWeb3SignatureAsync(request);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    // ── Cross-cutting: No cross-delegation ────────────────────

    [Fact]
    public async Task LocalSignInAsync_ShouldNotCallOtherSubServices()
    {
        // Arrange
        var request = new LocalSignInRequest { Email = "test@example.com", Password = "P@ss123!" };
        _localAuthServiceMock
            .Setup(x => x.LocalSignInAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInResponse { Success = true });

        // Act
        await _authService.LocalSignInAsync(request);

        // Assert — only local auth was called
        _oauthAuthServiceMock.VerifyNoOtherCalls();
        _passwordServiceMock.VerifyNoOtherCalls();
        _web3AuthServiceMock.VerifyNoOtherCalls();
    }
}

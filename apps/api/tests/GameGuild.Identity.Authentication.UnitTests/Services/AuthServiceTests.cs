using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IAuthenticationAttemptRepository> _authenticationAttemptRepositoryMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IRefreshTokenHasher> _refreshTokenHasherMock;
    private readonly Mock<IOAuthService> _oauthServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IWeb3Service> _web3ServiceMock;
    private readonly Mock<IEmailVerificationService> _emailVerificationServiceMock;
    private readonly Mock<IAuthenticationAnomalyDetectionService> _anomalyDetectionServiceMock;
    private readonly Mock<IUserEnumerationProtectionService> _enumerationProtectionMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _authenticationAttemptRepositoryMock = new Mock<IAuthenticationAttemptRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _refreshTokenHasherMock = new Mock<IRefreshTokenHasher>();
        _oauthServiceMock = new Mock<IOAuthService>();
        _configurationMock = new Mock<IConfiguration>();
        _web3ServiceMock = new Mock<IWeb3Service>();
        _emailVerificationServiceMock = new Mock<IEmailVerificationService>();
        _anomalyDetectionServiceMock = new Mock<IAuthenticationAnomalyDetectionService>();
        _enumerationProtectionMock = new Mock<IUserEnumerationProtectionService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        // Setup default configuration
        _configurationMock.Setup(x => x["Jwt:RefreshTokenExpiryInDays"]).Returns("7");

        // Setup HTTP context with mock IP address and user agent
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["User-Agent"] = "Test-User-Agent";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Setup enumeration protection defaults
        _enumerationProtectionMock
            .Setup(x => x.GetGenericErrorMessage(It.IsAny<string>()))
            .Returns("Authentication failed");

        // Setup anomaly detection to return low risk by default
        _anomalyDetectionServiceMock
            .Setup(x => x.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
            .ReturnsAsync(new AuthenticationAnomalyResult
            {
                IsAnomalous = false,
                RiskLevel = RiskLevel.Low,
                RiskScore = 10,
                DetectedAnomalies = []
            });

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _authenticationAttemptRepositoryMock.Object,
            _jwtTokenServiceMock.Object,
            _refreshTokenHasherMock.Object,
            _oauthServiceMock.Object,
            _configurationMock.Object,
            _web3ServiceMock.Object,
            _emailVerificationServiceMock.Object,
            _anomalyDetectionServiceMock.Object,
            _enumerationProtectionMock.Object,
            _httpContextAccessorMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task LocalSignInAsync_WithValidCredentials_ShouldReturnSuccessResponse()
    {
        // Arrange
        var email = "test@example.com";
        var password = "ValidPassword123!";
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = User.CreateWithPassword(email, "testuser", passwordHash);
        typeof(User).GetProperty("Id")!.SetValue(user, userId);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(userId, email, It.IsAny<string[]>(), tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(userId, It.IsAny<DeviceInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        var request = new LocalSignInRequest
        {
            Email = email,
            Password = password,
            TenantId = tenantId
        };

        // Act
        var result = await _authService.LocalSignInAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.UserId.Should().Be(userId);
        result.Email.Should().Be(email);
        result.RequiresStepUp.Should().BeFalse();

        _authenticationAttemptRepositoryMock.Verify(
            x => x.CreateAsync(It.Is<AuthenticationAttempt>(a => a.IsSuccessful && a.UserId == userId), default),
            Times.Once);
    }

    [Fact]
    public async Task LocalSignInAsync_WithInvalidPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var email = "test@example.com";
        var password = "ValidPassword123!";
        var wrongPassword = "WrongPassword123!";
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = User.CreateWithPassword(email, "testuser", passwordHash);
        typeof(User).GetProperty("Id")!.SetValue(user, userId);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new LocalSignInRequest
        {
            Email = email,
            Password = wrongPassword,
            TenantId = tenantId
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LocalSignInAsync(request));

        _authenticationAttemptRepositoryMock.Verify(
            x => x.CreateAsync(It.Is<AuthenticationAttempt>(a => !a.IsSuccessful), default),
            Times.Once);
    }

    [Fact]
    public async Task LocalSignInAsync_WithNonExistentUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var password = "Password123!";
        var tenantId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new LocalSignInRequest
        {
            Email = email,
            Password = password,
            TenantId = tenantId
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LocalSignInAsync(request));

        _authenticationAttemptRepositoryMock.Verify(
            x => x.CreateAsync(It.Is<AuthenticationAttempt>(a => !a.IsSuccessful), default),
            Times.Once);
    }

    [Fact]
    public async Task LocalSignInAsync_WithHighRiskDetection_ShouldRequireStepUp()
    {
        // Arrange
        var email = "test@example.com";
        var password = "ValidPassword123!";
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = User.CreateWithPassword(email, "testuser", passwordHash);
        typeof(User).GetProperty("Id")!.SetValue(user, userId);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Setup anomaly detection to return high risk
        _anomalyDetectionServiceMock
            .Setup(x => x.AnalyzeLoginAttemptAsync(It.IsAny<AuthenticationAttemptContext>()))
            .ReturnsAsync(new AuthenticationAnomalyResult
            {
                IsAnomalous = true,
                RiskLevel = RiskLevel.High,
                RiskScore = 75,
                DetectedAnomalies = ["Suspicious IP address", "New device"]
            });

        var request = new LocalSignInRequest
        {
            Email = email,
            Password = password,
            TenantId = tenantId
        };

        // Act
        var result = await _authService.LocalSignInAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.RequiresStepUp.Should().BeTrue();
        result.StepUpToken.Should().NotBeNullOrEmpty();
        result.RiskLevel.Should().Be(RiskLevel.High);
        result.RiskFactors.Should().Contain("Suspicious IP address");
        result.AvailableMethods.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LocalSignUpAsync_WithValidData_ShouldCreateUserAndReturnTokens()
    {
        // Arrange
        var email = "newuser@example.com";
        var password = "SecurePassword123!";
        var username = "newuser";
        var tenantId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessTokenAsync(It.IsAny<Guid>(), email, It.IsAny<string[]>(), tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(It.IsAny<Guid>(), It.IsAny<DeviceInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refresh-token");

        var request = new LocalSignUpRequest
        {
            Email = email,
            Password = password,
            Username = username,
            TenantId = tenantId
        };

        // Act
        var result = await _authService.LocalSignUpAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.Email.Should().Be(email);
        result.UserId.Should().NotBeEmpty();

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Once);
        _userRepositoryMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LocalSignUpAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var email = "existing@example.com";
        var password = "Password123!";
        var username = "existinguser";
        var tenantId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.ExistsByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new LocalSignUpRequest
        {
            Email = email,
            Password = password,
            Username = username,
            TenantId = tenantId
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LocalSignUpAsync(request));

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var refreshToken = "valid-refresh-token";
        var hashedToken = "hashed-token";

        var storedToken = new RefreshToken
        {
            UserId = userId,
            Token = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _refreshTokenHasherMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns(hashedToken);

        _refreshTokenRepositoryMock
            .Setup(x => x.GetByTokenAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // RefreshTokenAsync uses synchronous GenerateAccessToken, not async
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .Returns("new-access-token");

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshTokenAsync(userId, It.IsAny<DeviceInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-refresh-token");

        _refreshTokenRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken,
            TenantId = tenantId
        };

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");
        result.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "expired-refresh-token";
        var hashedToken = "hashed-token";
        var tenantId = Guid.NewGuid();

        var storedToken = new RefreshToken
        {
            UserId = userId,
            Token = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired
            IsRevoked = false
        };

        _refreshTokenHasherMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns(hashedToken);

        _refreshTokenRepositoryMock
            .Setup(x => x.GetByTokenAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken,
            TenantId = tenantId
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.RefreshTokenAsync(request));
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "revoked-refresh-token";
        var hashedToken = "hashed-token";
        var tenantId = Guid.NewGuid();

        var storedToken = new RefreshToken
        {
            UserId = userId,
            Token = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true // Revoked
        };

        _refreshTokenHasherMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns(hashedToken);

        _refreshTokenRepositoryMock
            .Setup(x => x.GetByTokenAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        var request = new RefreshTokenRequest
        {
            RefreshToken = refreshToken,
            TenantId = tenantId
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.RefreshTokenAsync(request));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var refreshToken = "valid-refresh-token";
        var hashedToken = "hashed-token";
        var ipAddress = "192.168.1.1";

        var storedToken = new RefreshToken
        {
            UserId = userId,
            Token = hashedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _refreshTokenHasherMock
            .Setup(x => x.HashToken(refreshToken))
            .Returns(hashedToken);

        _refreshTokenRepositoryMock
            .Setup(x => x.GetByTokenAsync(hashedToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        // Act
        await _authService.RevokeRefreshTokenAsync(refreshToken, ipAddress);

        // Assert
        storedToken.IsRevoked.Should().BeTrue();
        storedToken.RevokedByIp.Should().Be(ipAddress);

        _refreshTokenRepositoryMock.Verify(
            x => x.UpdateAsync(storedToken, default),
            Times.Once);
    }
}

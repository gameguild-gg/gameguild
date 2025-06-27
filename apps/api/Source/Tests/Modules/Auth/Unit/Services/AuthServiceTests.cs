using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using GameGuild.Data;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.Auth.Services;
using GameGuild.Modules.User.Models;
using GameGuild.Modules.Tenant.Services;
using GameGuild.Modules.Tenant.Models;
using System.Security.Claims;


namespace GameGuild.Tests.Modules.Auth.Unit.Services;

public class AuthServiceTests : IDisposable {
  private readonly ApplicationDbContext _context;

  private readonly Mock<IJwtTokenService> _mockJwtService;

  private readonly Mock<IOAuthService> _mockOAuthService;

  private readonly Mock<IWeb3Service> _mockWeb3Service;

  private readonly Mock<IEmailVerificationService> _mockEmailService;

  private readonly Mock<IConfiguration> _mockConfiguration;

  private readonly Mock<ITenantAuthService> _mockTenantAuthService;

  private readonly Mock<ITenantService> _mockTenantService;

  private readonly AuthService _authService;

  public AuthServiceTests() {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                  .Options;

    _context = new ApplicationDbContext(options);
    _mockJwtService = new Mock<IJwtTokenService>();
    _mockOAuthService = new Mock<IOAuthService>();
    _mockWeb3Service = new Mock<IWeb3Service>();
    _mockEmailService = new Mock<IEmailVerificationService>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockTenantAuthService = new Mock<ITenantAuthService>();
    _mockTenantService = new Mock<ITenantService>();

    // Setup default JWT service behavior
    _mockJwtService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

    // Setup default tenant service behavior
    _mockTenantService.Setup(x => x.AddUserToTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                      .ReturnsAsync(new TenantPermission());

    _authService = new AuthService(
      _context,
      _mockJwtService.Object,
      _mockOAuthService.Object,
      _mockConfiguration.Object,
      _mockWeb3Service.Object,
      _mockEmailService.Object,
      _mockTenantAuthService.Object,
      _mockTenantService.Object
    );
  }

  [Fact]
  public async Task LocalSignUpAsync_ValidRequest_CreatesUserAndCredential() {
    // Arrange
    var tenantId = Guid.NewGuid();
    var request = new LocalSignUpRequestDto { Email = "test@example.com", Password = "P455W0RD", Username = "testuser", TenantId = tenantId };

    _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<UserDto>(), It.IsAny<string[]>()))
                   .Returns("mock-access-token");

    var enhancedResponse = new SignInResponseDto {
      AccessToken = "tenant-enhanced-token",
      RefreshToken = "mock-refresh-token",
      User = new UserDto { Email = "test@example.com", Username = "testuser" },
      TenantId = tenantId,
      AvailableTenants = new List<TenantInfoDto> { new TenantInfoDto { Id = tenantId, Name = "Test Tenant", IsActive = true } },
    };

    _mockTenantAuthService
      .Setup(x => x.EnhanceWithTenantDataAsync(It.IsAny<SignInResponseDto>(), It.IsAny<User>(), tenantId))
      .ReturnsAsync(enhancedResponse);

    // Act
    var result = await _authService.LocalSignUpAsync(request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("tenant-enhanced-token", result.AccessToken);
    Assert.Equal("test@example.com", result.User.Email);
    Assert.Equal(tenantId, result.TenantId);

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    Assert.NotNull(user);

    var credential = await _context.Credentials.FirstOrDefaultAsync(c => c.UserId == user.Id);
    Assert.NotNull(credential);
    Assert.Equal("password", credential.Type);

    // Verify tenant service was called
    _mockTenantService.Verify(ts => ts.AddUserToTenantAsync(user.Id, tenantId), Times.Once);
  }

  [Fact]
  public async Task LocalSignUpAsync_DuplicateEmail_ThrowsInvalidOperationException() {
    // Arrange
    var existingUser = new User { Email = "test@example.com", Name = "Existing User" };
    _context.Users.Add(existingUser);
    await _context.SaveChangesAsync();

    var request = new LocalSignUpRequestDto { Email = "test@example.com", Password = "P455W0RD", Username = "testuser" };

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.LocalSignUpAsync(request));
  }

  [Fact]
  public async Task LocalSignInAsync_ValidCredentials_ReturnsSignInResponse() {
    // Arrange
    var user = new User { Email = "test@example.com", Name = "Test User" };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    var hashedPassword =
      Convert.ToBase64String(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("P455W0RD"))
      );
    var credential = new Credential { UserId = user.Id, Type = "password", Value = hashedPassword, IsActive = true };
    _context.Credentials.Add(credential);
    await _context.SaveChangesAsync();

    var tenantId = Guid.NewGuid();
    var request = new LocalSignInRequestDto { Email = "test@example.com", Password = "P455W0RD", TenantId = tenantId };

    _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<UserDto>(), It.IsAny<string[]>()))
                   .Returns("mock-access-token");

    var enhancedResponse = new SignInResponseDto {
      AccessToken = "tenant-enhanced-token",
      RefreshToken = "mock-refresh-token",
      User = new UserDto { Id = user.Id, Email = user.Email, Username = user.Name },
      TenantId = tenantId,
      AvailableTenants = new List<TenantInfoDto> { new TenantInfoDto { Id = tenantId, Name = "Test Tenant", IsActive = true } },
    };

    _mockTenantAuthService
      .Setup(x => x.EnhanceWithTenantDataAsync(It.IsAny<SignInResponseDto>(), It.IsAny<User>(), tenantId))
      .ReturnsAsync(enhancedResponse);

    // Act
    var result = await _authService.LocalSignInAsync(request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("tenant-enhanced-token", result.AccessToken);
    Assert.Equal("test@example.com", result.User.Email);
    Assert.Equal(tenantId, result.TenantId);
    Assert.NotNull(result.AvailableTenants);
    Assert.Single(result.AvailableTenants);
  }

  [Fact]
  public async Task LocalSignInAsync_InvalidCredentials_ThrowsUnauthorizedAccessException() {
    // Arrange
    var request = new LocalSignInRequestDto { Email = "nonexistent@example.com", Password = "P455W0RD" };

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LocalSignInAsync(request));
  }

  [Fact]
  public async Task LocalSignInAsync_InvalidPassword_ThrowsUnauthorizedAccessException() {
    // Arrange
    var user = new User { Email = "test@example.com", Name = "Test User" };
    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    var hashedPassword =
      Convert.ToBase64String(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("correctpassword"))
      );
    var credential = new Credential { UserId = user.Id, Type = "password", Value = hashedPassword, IsActive = true };
    _context.Credentials.Add(credential);
    await _context.SaveChangesAsync();

    var request = new LocalSignInRequestDto { Email = "test@example.com", Password = "wrongpassword" };

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LocalSignInAsync(request));
  }

  [Fact]
  public async Task GenerateWeb3ChallengeAsync_ValidRequest_ReturnsChallenge() {
    // Arrange
    var request = new Web3ChallengeRequestDto { WalletAddress = "0x742d35Cc6634C0532925a3b8D".ToLower(), ChainId = "1" };

    var expectedResponse = new Web3ChallengeResponseDto { Challenge = "mock-challenge", Nonce = "mock-nonce", ExpiresAt = DateTime.UtcNow.AddMinutes(15) };

    _mockWeb3Service.Setup(x => x.GenerateChallengeAsync(request)).ReturnsAsync(expectedResponse);

    // Act
    var result = await _authService.GenerateWeb3ChallengeAsync(request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("mock-challenge", result.Challenge);
    Assert.Equal("mock-nonce", result.Nonce);
  }

  [Fact]
  public async Task VerifyWeb3SignatureAsync_ValidSignature_ReturnsSignInResponse() {
    // Arrange
    var tenantId = Guid.NewGuid();

    var request = new Web3VerifyRequestDto {
      WalletAddress = "0x742d35Cc6634C0532925a3b8D".ToLower(),
      Signature = "mock-signature",
      Nonce = "mock-nonce",
      ChainId = "1",
      TenantId = tenantId,
    };

    var user = new User { Email = "web3user@example.com", Name = "Web3 User" };

    _mockWeb3Service.Setup(x => x.VerifySignatureAsync(request)).ReturnsAsync(true);

    _mockWeb3Service.Setup(x => x.FindOrCreateWeb3UserAsync(request.WalletAddress, request.ChainId)).ReturnsAsync(user);

    _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<UserDto>(), It.IsAny<string[]>()))
                   .Returns("mock-access-token");

    _mockJwtService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

    var enhancedResponse = new SignInResponseDto {
      AccessToken = "tenant-enhanced-token",
      RefreshToken = "mock-refresh-token",
      User = new UserDto { Id = user.Id, Email = user.Email, Username = user.Name },
      TenantId = tenantId,
      AvailableTenants = new List<TenantInfoDto> { new TenantInfoDto { Id = tenantId, Name = "Test Tenant", IsActive = true } },
    };

    _mockTenantAuthService
      .Setup(x => x.EnhanceWithTenantDataAsync(It.IsAny<SignInResponseDto>(), It.IsAny<User>(), tenantId))
      .ReturnsAsync(enhancedResponse);

    // Act
    var result = await _authService.VerifyWeb3SignatureAsync(request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("tenant-enhanced-token", result.AccessToken);
    Assert.Equal("mock-refresh-token", result.RefreshToken);
    Assert.Equal(tenantId, result.TenantId);
    Assert.NotNull(result.AvailableTenants);
  }

  [Fact]
  public async Task VerifyWeb3SignatureAsync_InvalidSignature_ThrowsUnauthorizedAccessException() {
    // Arrange
    var request = new Web3VerifyRequestDto { WalletAddress = "0x742d35Cc6634C0532925a3b8D".ToLower(), Signature = "invalid-signature", Nonce = "mock-nonce", ChainId = "1" };

    _mockWeb3Service.Setup(x => x.VerifySignatureAsync(request)).ReturnsAsync(false);

    // Act & Assert
    await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.VerifyWeb3SignatureAsync(request));
  }

  [Fact]
  public async Task Web3SignIn_WithInvalidTenant_FallsBackToDefaultTenant() {
    // Arrange
    var invalidTenantId = Guid.NewGuid();
    var defaultTenantId = Guid.NewGuid();

    var request = new Web3VerifyRequestDto {
      WalletAddress = "0x742d35Cc6634C0532925a3b8D".ToLower(),
      Signature = "mock-signature",
      Nonce = "mock-nonce",
      ChainId = "1",
      TenantId = invalidTenantId,
    };

    var user = new User { Email = "web3user@example.com", Name = "Web3 User" };

    _mockWeb3Service.Setup(x => x.VerifySignatureAsync(request)).ReturnsAsync(true);

    _mockWeb3Service.Setup(x => x.FindOrCreateWeb3UserAsync(request.WalletAddress, request.ChainId)).ReturnsAsync(user);

    _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<UserDto>(), It.IsAny<string[]>()))
                   .Returns("mock-access-token");

    _mockJwtService.Setup(x => x.GenerateRefreshToken()).Returns("mock-refresh-token");

    // Set up tenant auth service to fall back to default tenant
    var enhancedResponse = new SignInResponseDto {
      AccessToken = "tenant-enhanced-token",
      RefreshToken = "mock-refresh-token",
      User = new UserDto { Id = user.Id, Email = user.Email, Username = user.Name },
      TenantId = defaultTenantId, // Note: different from requested tenant ID
      AvailableTenants = new List<TenantInfoDto> { new TenantInfoDto { Id = defaultTenantId, Name = "Default Tenant", IsActive = true } },
    };

    _mockTenantAuthService
      .Setup(x => x.EnhanceWithTenantDataAsync(It.IsAny<SignInResponseDto>(), It.IsAny<User>(), invalidTenantId))
      .ReturnsAsync(enhancedResponse);

    // Act
    var result = await _authService.VerifyWeb3SignatureAsync(request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("tenant-enhanced-token", result.AccessToken);
    Assert.Equal(defaultTenantId, result.TenantId); // Should use default tenant
    Assert.NotEqual(invalidTenantId, result.TenantId); // Should not use invalid tenant
    Assert.NotNull(result.AvailableTenants);
    Assert.Single(result.AvailableTenants);
    Assert.Equal("Default Tenant", result.AvailableTenants.First().Name);
  }

  [Fact]
  public async Task RefreshTokenAsync_WithTenantId_IncludesTenantClaims() {
    // Arrange
    var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", Name = "Test User" };
    _context.Users.Add(user);

    var refreshToken = new GameGuild.Modules.Auth.Models.RefreshToken {
      UserId = user.Id, Token = "valid-refresh-token", ExpiresAt = DateTime.UtcNow.AddDays(7), IsRevoked = false, // IsActive is calculated from !IsRevoked && !IsExpired
    };

    _context.RefreshTokens.Add(refreshToken);
    await _context.SaveChangesAsync();

    var tenantId = Guid.NewGuid();
    var request = new RefreshTokenRequestDto { RefreshToken = "valid-refresh-token", TenantId = tenantId };

    var tenantClaims = new List<Claim> { new Claim("tenant_id", tenantId.ToString()), new Claim("tenant_permission_flags1", "1"), new Claim("tenant_permission_flags2", "2") };

    _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<UserDto>(), It.IsAny<string[]>()))
                   .Returns("new-access-token");

    _mockJwtService
      .Setup(x => x.GenerateAccessToken(It.IsAny<UserDto>(), It.IsAny<string[]>(), It.IsAny<IEnumerable<Claim>>()))
      .Returns("new-access-token-with-tenant-claims");

    _mockJwtService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-token");

    // Setup GetUserTenantsAsync to return a list with a valid tenant permission
    var tenantPermissions = new List<TenantPermission> {
      new TenantPermission {
        Id = Guid.NewGuid(), UserId = user.Id, TenantId = tenantId, ExpiresAt = null, // Not expired = IsValid will be true
      },
    };

    _mockTenantAuthService.Setup(x => x.GetUserTenantsAsync(It.IsAny<User>())).ReturnsAsync(tenantPermissions);

    _mockTenantAuthService.Setup(x => x.GetTenantClaimsAsync(It.IsAny<User>(), tenantId)).ReturnsAsync(tenantClaims);

    // Act
    var result = await _authService.RefreshTokenAsync(request);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("new-access-token-with-tenant-claims", result.AccessToken);
    Assert.Equal("new-refresh-token", result.RefreshToken);
    Assert.Equal(tenantId, result.TenantId);

    // Verify old refresh token is invalidated
    var oldToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
    Assert.NotNull(oldToken);
    Assert.True(oldToken.IsRevoked); // Check that the token has been revoked instead of checking IsActive

    // Verify tenant claims were requested
    _mockTenantAuthService.Verify(x => x.GetUserTenantsAsync(It.IsAny<User>()), Times.Once);
    _mockTenantAuthService.Verify(x => x.GetTenantClaimsAsync(It.IsAny<User>(), tenantId), Times.Once);

    // Verify token service was called with tenant claims
    _mockJwtService.Verify(
      x => x.GenerateAccessToken(
        It.IsAny<UserDto>(),
        It.IsAny<string[]>(),
        It.Is<IEnumerable<Claim>>(c =>
                                    c.Any(claim => claim.Type == "tenant_id" && claim.Value == tenantId.ToString())
        )
      ),
      Times.Once
    );
  }

  public void Dispose() { _context.Dispose(); }
}

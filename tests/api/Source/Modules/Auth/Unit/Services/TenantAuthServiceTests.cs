using System.Security.Claims;
using GameGuild.Data;
using GameGuild.Modules.Authentication.Constants;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Authentication.Services;
using GameGuild.Modules.Tenants.Models;
using GameGuild.Modules.Tenants.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using UserModel = GameGuild.Modules.Users.Models.User;


namespace GameGuild.API.Tests.Modules.Auth.Unit.Services;

public class TenantAuthServiceTests : IDisposable {
  private readonly ApplicationDbContext _context;

  private readonly Mock<ITenantService> _mockTenantService;

  private readonly Mock<ITenantContextService> _mockTenantContextService;

  private readonly Mock<IJwtTokenService> _mockJwtTokenService;

  private readonly TenantAuthService _tenantAuthService;

  public TenantAuthServiceTests() {
    // Set up in-memory database
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                  .UseInMemoryDatabase(Guid.NewGuid().ToString())
                  .Options;

    _context = new ApplicationDbContext(options);

    // Set up mocks
    _mockTenantService = new Mock<ITenantService>();
    _mockTenantContextService = new Mock<ITenantContextService>();
    _mockJwtTokenService = new Mock<IJwtTokenService>();

    // Create service instance
    _tenantAuthService = new TenantAuthService(
      _mockTenantService.Object,
      _mockTenantContextService.Object,
      _mockJwtTokenService.Object
    );
  }

  [Fact]
  public async Task EnhanceWithTenantDataAsync_WithValidTenant_AddsTenantInfoToResponse() {
    // Arrange
    var user = new UserModel { Id = Guid.NewGuid(), Name = "testuser", Email = "test@example.com" };

    var tenantId = Guid.NewGuid();

    var tenantPermissions = new List<TenantPermission> {
      new TenantPermission {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        TenantId = tenantId,
        // Initialize as not expired and not deleted for IsValid to be true
        ExpiresAt = null, // Not expired
        // IsDeleted is false by default
        Tenant = new Tenant { Id = tenantId, Name = "Test Tenant", IsActive = true },
      },
    };

    _mockTenantService.Setup(x => x.GetTenantsForUserAsync(user.Id)).ReturnsAsync(tenantPermissions);

    var claims = new List<Claim> { new Claim(JwtClaimTypes.TenantId, tenantId.ToString()), new Claim(JwtClaimTypes.TenantPermissionFlags1, "1"), new Claim(JwtClaimTypes.TenantPermissionFlags2, "2") };

    _mockTenantContextService.Setup(x => x.GetTenantPermissionAsync(user.Id, tenantId))
                             .ReturnsAsync(tenantPermissions.First());

    _mockJwtTokenService
      .Setup(x => x.GenerateAccessToken(It.IsAny<UserDto>(), It.IsAny<string[]>(), It.IsAny<IEnumerable<Claim>>()))
      .Returns("enhanced-token-with-tenant-claims");

    var authResult = new SignInResponseDto { AccessToken = "original-token", RefreshToken = "refresh-token", User = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email } };

    // Act
    var result = await _tenantAuthService.EnhanceWithTenantDataAsync(authResult, user, tenantId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("enhanced-token-with-tenant-claims", result.AccessToken);
    Assert.Equal("refresh-token", result.RefreshToken);
    Assert.Equal(tenantId, result.TenantId);
    Assert.NotNull(result.AvailableTenants);
    Assert.Single(result.AvailableTenants);
    Assert.Equal(tenantId, result.AvailableTenants.First().Id);
    Assert.Equal("Test Tenant", result.AvailableTenants.First().Name);
    Assert.True(result.AvailableTenants.First().IsActive);
  }

  [Fact]
  public async Task EnhanceWithTenantDataAsync_WithNoAvailableTenants_ReturnsOriginalResponse() {
    // Arrange
    var user = new UserModel { Id = Guid.NewGuid(), Name = "testuser", Email = "test@example.com" };

    // Empty tenant list
    _mockTenantService.Setup(x => x.GetTenantsForUserAsync(user.Id)).ReturnsAsync(new List<TenantPermission>());

    var authResult = new SignInResponseDto { AccessToken = "original-token", RefreshToken = "refresh-token", User = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email } };

    // Act
    var result = await _tenantAuthService.EnhanceWithTenantDataAsync(authResult, user);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("original-token", result.AccessToken);
    Assert.Equal("refresh-token", result.RefreshToken);
    Assert.Null(result.TenantId);
    Assert.Null(result.AvailableTenants);

    // Verify token was not regenerated
    _mockJwtTokenService.Verify(
      x => x.GenerateAccessToken(It.IsAny<UserDto>(), It.IsAny<string[]>(), It.IsAny<IEnumerable<Claim>>()),
      Times.Never
    );
  }

  [Fact]
  public async Task GetTenantClaimsAsync_WithValidTenant_ReturnsClaimsWithPermissions() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();

    var tenantPermission = new TenantPermission { UserId = userId, TenantId = tenantId, PermissionFlags1 = 42, PermissionFlags2 = 24 };

    _mockTenantContextService.Setup(x => x.GetTenantPermissionAsync(userId, tenantId)).ReturnsAsync(tenantPermission);

    var user = new UserModel { Id = userId };

    // Act
    var claims = await _tenantAuthService.GetTenantClaimsAsync(user, tenantId);

    // Assert
    Assert.NotNull(claims);
    var claimsList = claims.ToList();
    Assert.Equal(3, claimsList.Count);
    Assert.Equal(JwtClaimTypes.TenantId, claimsList[0].Type);
    Assert.Equal(tenantId.ToString(), claimsList[0].Value);
    Assert.Equal(JwtClaimTypes.TenantPermissionFlags1, claimsList[1].Type);
    Assert.Equal("42", claimsList[1].Value);
    Assert.Equal(JwtClaimTypes.TenantPermissionFlags2, claimsList[2].Type);
    Assert.Equal("24", claimsList[2].Value);
  }

  public void Dispose() { _context.Dispose(); }
}

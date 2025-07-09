using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameGuild.Modules.Authentication.Dtos;
using GameGuild.Modules.Authentication.Services;
using Microsoft.Extensions.Configuration;

namespace GameGuild.API.Tests.Modules.Auth.Unit.Services;

public class JwtTokenServiceTests {
  private readonly JwtTokenService _jwtTokenService;

  private readonly IConfiguration _configuration;

  public JwtTokenServiceTests() {
    var configData = new Dictionary<string, string> {
      { "Jwt:SecretKey", "your-very-long-secret-key-for-testing-purposes-at-least-256-bits" },
      { "Jwt:Issuer", "test-issuer" },
      { "Jwt:Audience", "test-audience" },
      { "Jwt:ExpiryInMinutes", "15" },
      { "Jwt:RefreshTokenExpiryInDays", "7" },
    };

    _configuration = new ConfigurationBuilder().AddInMemoryCollection(configData!).Build();

    _jwtTokenService = new JwtTokenService(_configuration);
  }

  [Fact]
  public void GenerateAccessToken_ValidUser_ReturnsValidJwtToken() {
    // Arrange
    var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" };
    var roles = new[] { "User", "Admin" };

    // Act
    var token = _jwtTokenService.GenerateAccessToken(user, roles);

    // Assert
    Assert.NotNull(token);
    Assert.NotEmpty(token);

    // Verify token structure
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(token);

    Assert.Equal("test-issuer", jsonToken.Issuer);
    Assert.Contains(jsonToken.Audiences, a => a == "test-audience");
    Assert.Equal(user.Id.ToString(), jsonToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
    Assert.Equal(user.Email, jsonToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    Assert.Equal(user.Username, jsonToken.Claims.First(c => c.Type == "username").Value);
  }

  [Fact]
  public void ValidateToken_ValidToken_ReturnsClaimsPrincipal() {
    // Arrange
    var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" };
    var roles = new[] { "User" };
    var token = _jwtTokenService.GenerateAccessToken(user, roles);

    // Act
    var principal = _jwtTokenService.ValidateToken(token);

    // Assert
    Assert.NotNull(principal);
    Assert.True(principal.Identity?.IsAuthenticated);

    // Debug: Check all claims to understand what's available
    var allClaims = principal.Claims.ToList();
    var subClaim = allClaims.FirstOrDefault(c =>
                                              c.Type is JwtRegisteredClaimNames.Sub
                                                        or "sub"
                                                        or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    );
    var emailClaim = allClaims.FirstOrDefault(c =>
                                                c.Type is JwtRegisteredClaimNames.Email
                                                          or "email"
                                                          or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
    );

    // Debug output - remove after fixing
    // foreach (var claim in allClaims)
    // {
    //     Console.WriteLine($"Claim Type: '{claim.Type}', Value: '{claim.Value}'");
    // }
    Assert.NotNull(subClaim);
    Assert.NotNull(emailClaim);
    Assert.Equal(user.Id.ToString(), subClaim.Value);
    Assert.Equal(user.Email, emailClaim.Value);
  }

  [Fact]
  public void ValidateToken_InvalidToken_ReturnsNull() {
    // Arrange
    var invalidToken = "invalid-token";

    // Act
    var principal = _jwtTokenService.ValidateToken(invalidToken);

    // Assert
    Assert.Null(principal);
  }

  [Fact]
  public void ValidateToken_ExpiredToken_ReturnsNull() {
    // Arrange - Create a token that expires well beyond the clock skew tolerance
    var expiredConfigData = new Dictionary<string, string> {
      { "Jwt:SecretKey", "your-very-long-secret-key-for-testing-purposes-at-least-256-bits" },
      { "Jwt:Issuer", "test-issuer" },
      { "Jwt:Audience", "test-audience" },
      { "Jwt:ExpiryInMinutes", "-10" }, // Expired by 10 minutes (beyond 5-minute clock skew)
      { "Jwt:RefreshTokenExpiryInDays", "7" },
    };

    var expiredConfig = new ConfigurationBuilder().AddInMemoryCollection(expiredConfigData!).Build();

    var expiredTokenService = new JwtTokenService(expiredConfig);

    var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" };
    var roles = new[] { "User" };
    var expiredToken = expiredTokenService.GenerateAccessToken(user, roles);

    // Act
    var principal = _jwtTokenService.ValidateToken(expiredToken);

    // Assert
    Assert.Null(principal);
  }

  [Fact]
  public void GetPrincipalFromExpiredToken_ExpiredToken_ReturnsClaimsPrincipal() {
    // Arrange - Create a token that expires immediately (OK for this test since ValidateLifetime = false)
    var expiredConfigData = new Dictionary<string, string> {
      { "Jwt:SecretKey", "your-very-long-secret-key-for-testing-purposes-at-least-256-bits" },
      { "Jwt:Issuer", "test-issuer" },
      { "Jwt:Audience", "test-audience" },
      { "Jwt:ExpiryInMinutes", "-1" }, // Expired
      { "Jwt:RefreshTokenExpiryInDays", "7" },
    };

    var expiredConfig = new ConfigurationBuilder().AddInMemoryCollection(expiredConfigData!).Build();

    var expiredTokenService = new JwtTokenService(expiredConfig);

    var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" };
    var roles = new[] { "User" };
    var expiredToken = expiredTokenService.GenerateAccessToken(user, roles);

    // Act
    var principal = _jwtTokenService.GetPrincipalFromExpiredToken(expiredToken);

    // Assert
    Assert.NotNull(principal);
    var subClaim = principal.Claims.FirstOrDefault(c =>
                                                     c.Type is JwtRegisteredClaimNames.Sub
                                                               or "sub"
                                                               or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    );
    var emailClaim = principal.Claims.FirstOrDefault(c =>
                                                       c.Type is JwtRegisteredClaimNames.Email
                                                                 or "email"
                                                                 or "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
    );

    Assert.Equal(user.Id.ToString(), subClaim?.Value);
    Assert.Equal(user.Email, emailClaim?.Value);
  }

  [Fact]
  public void GenerateRefreshToken_ReturnsSecureToken() {
    // Act
    var refreshToken1 = _jwtTokenService.GenerateRefreshToken();
    var refreshToken2 = _jwtTokenService.GenerateRefreshToken();

    // Assert
    Assert.NotNull(refreshToken1);
    Assert.NotNull(refreshToken2);
    Assert.NotEmpty(refreshToken1);
    Assert.NotEmpty(refreshToken2);
    Assert.NotEqual(refreshToken1, refreshToken2); // Should be unique
    Assert.True(refreshToken1.Length >= 32); // Should be reasonably long
  }

  [Fact]
  public void GenerateAccessToken_MultipleRoles_IncludesAllRoles() {
    // Arrange
    var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" };
    var roles = new[] { "User", "Admin", "Moderator" };

    // Act
    var token = _jwtTokenService.GenerateAccessToken(user, roles);

    // Assert
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(token);

    var roleClaims = jsonToken.Claims.Where(c => c.Type == ClaimTypes.Role).ToArray();
    Assert.Equal(3, roleClaims.Length);
    Assert.Contains(roleClaims, c => c.Value == "User");
    Assert.Contains(roleClaims, c => c.Value == "Admin");
    Assert.Contains(roleClaims, c => c.Value == "Moderator");
  }

  [Fact]
  public void GenerateAccessToken_EmptyRoles_CreatesTokenWithoutRoles() {
    // Arrange
    var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" };
    string[] roles = [];

    // Act
    var token = _jwtTokenService.GenerateAccessToken(user, roles);

    // Assert
    Assert.NotNull(token);
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(token);

    var roleClaims = jsonToken.Claims.Where(c => c.Type == ClaimTypes.Role);
    Assert.Empty(roleClaims);
  }

  [Fact]
  public void GenerateAccessToken_WithTenantClaims_IncludesClaimsInToken() {
    // Arrange
    var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" };
    var roles = new[] { "User" };

    // Create tenant claims
    var tenantId = Guid.NewGuid();
    var tenantClaims = new List<Claim> { new Claim("tenant_id", tenantId.ToString()), new Claim("tenant_permission_flags1", "42"), new Claim("tenant_permission_flags2", "24") };

    // Act
    var token = _jwtTokenService.GenerateAccessToken(user, roles, tenantClaims);

    // Assert
    Assert.NotNull(token);
    var handler = new JwtSecurityTokenHandler();
    var jsonToken = handler.ReadJwtToken(token);

    // Verify tenant claims are present
    var tenantIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "tenant_id");
    Assert.NotNull(tenantIdClaim);
    Assert.Equal(tenantId.ToString(), tenantIdClaim.Value);

    var permissionFlags1Claim = jsonToken.Claims.FirstOrDefault(c => c.Type == "tenant_permission_flags1");
    Assert.NotNull(permissionFlags1Claim);
    Assert.Equal("42", permissionFlags1Claim.Value);

    var permissionFlags2Claim = jsonToken.Claims.FirstOrDefault(c => c.Type == "tenant_permission_flags2");
    Assert.NotNull(permissionFlags2Claim);
    Assert.Equal("24", permissionFlags2Claim.Value);
  }

  [Fact]
  public void GetPrincipalFromExpiredToken_WithTenantClaims_PreservesTenantClaims() {
    // Arrange - Create a token that expires immediately but has tenant claims (OK for this test since ValidateLifetime = false)
    var expiredConfigData = new Dictionary<string, string> {
      { "Jwt:SecretKey", "your-very-long-secret-key-for-testing-purposes-at-least-256-bits" },
      { "Jwt:Issuer", "test-issuer" },
      { "Jwt:Audience", "test-audience" },
      { "Jwt:ExpiryInMinutes", "-1" }, // Expired
      { "Jwt:RefreshTokenExpiryInDays", "7" },
    };

    var expiredConfig = new ConfigurationBuilder().AddInMemoryCollection(expiredConfigData!).Build();

    var expiredTokenService = new JwtTokenService(expiredConfig);

    var user = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com" };
    var roles = new[] { "User" };

    // Create tenant claims
    var tenantId = Guid.NewGuid();
    var tenantClaims = new List<Claim> { new Claim("tenant_id", tenantId.ToString()), new Claim("tenant_permission_flags1", "42"), new Claim("tenant_permission_flags2", "24") };

    var expiredToken = expiredTokenService.GenerateAccessToken(user, roles, tenantClaims);

    // Act
    var principal = _jwtTokenService.GetPrincipalFromExpiredToken(expiredToken);

    // Assert
    Assert.NotNull(principal);

    // Verify tenant claims are preserved
    var tenantIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "tenant_id");
    Assert.NotNull(tenantIdClaim);
    Assert.Equal(tenantId.ToString(), tenantIdClaim.Value);

    var permissionFlags1Claim = principal.Claims.FirstOrDefault(c => c.Type == "tenant_permission_flags1");
    Assert.NotNull(permissionFlags1Claim);
    Assert.Equal("42", permissionFlags1Claim.Value);

    var permissionFlags2Claim = principal.Claims.FirstOrDefault(c => c.Type == "tenant_permission_flags2");
    Assert.NotNull(permissionFlags2Claim);
    Assert.Equal("24", permissionFlags2Claim.Value);
  }
}

using System.Globalization;
using System.Security.Claims;
using GameGuild.Authorization.Identity;
using GameGuild.Authorization.Middleware;
using GameGuild.Core.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;


namespace GameGuild.Tests.Common.Middleware;

/// <summary>
/// Tests for the ContextMiddleware and related services
/// </summary>
public class ContextMiddlewareTests {
  private readonly IServiceProvider _serviceProvider;
  private readonly ILogger<ContextMiddleware> _logger;

  public ContextMiddlewareTests() {
    var services = new ServiceCollection();
    services.AddLogging();
    _serviceProvider = services.BuildServiceProvider();
    _logger = _serviceProvider.GetRequiredService<ILogger<ContextMiddleware>>();
  }

  [Fact]
  public async Task ContextMiddleware_SetsUserAndTenantContext_Successfully() {
    // Arrange
    var testUserId = Guid.NewGuid().ToString();
    var httpContext = CreateHttpContextWithUser(testUserId, "test@example.com", "TestTenant");
    var middleware = new ContextMiddleware(
        next: (context) => Task.CompletedTask,
        logger: _logger
    );

    // Create context services with the test HTTP context
    var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext };
    var userLogger = new Mock<ILogger<UserContext>>();
    var tenantLogger = new Mock<ILogger<TenantContext>>();
    var userContext = new UserContext(httpContextAccessor, userLogger.Object);
    var tenantContext = new TenantContext(httpContextAccessor, tenantLogger.Object);

    // Create mock contexts for the additional parameters
    var permissionsContext = new Mock<IPermissionsContext>();
    permissionsContext.Setup(x => x.IsSystemAdmin).Returns(false);
    permissionsContext.Setup(x => x.IsTenantAdmin).Returns(false);
    permissionsContext.Setup(x => x.IsAuthenticated).Returns(true);
    permissionsContext.Setup(x => x.UserId).Returns(Guid.Parse(testUserId));
    permissionsContext.Setup(x => x.TenantId).Returns((Guid?)null);

    var resourceContext = new Mock<IResourceContext>();
    resourceContext.Setup(x => x.ResourceId).Returns((Guid?)null);
    resourceContext.Setup(x => x.ResourceType).Returns((string?)null);
    resourceContext.Setup(x => x.GetResourceIdentifier()).Returns("test-resource");

    var localizationContext = new Mock<ILocalizationContext>();
    localizationContext.Setup(x => x.CurrentCulture).Returns(System.Globalization.CultureInfo.InvariantCulture);
    localizationContext.Setup(x => x.TimeZoneId).Returns("UTC");
    localizationContext.Setup(x => x.CurrentTimeZone).Returns(TimeZoneInfo.Utc);
    localizationContext.Setup(x => x.GetCurrentLocalTime()).Returns(DateTime.UtcNow);

    // Act
    await middleware.InvokeAsync(httpContext, userContext, tenantContext, permissionsContext.Object, resourceContext.Object, localizationContext.Object);

    // Assert
    Assert.True(userContext.IsAuthenticated);
    Assert.Equal(Guid.Parse(testUserId), userContext.UserId);
    Assert.Equal("test@example.com", userContext.Email);
    Assert.NotNull(httpContext.Items["UserContext"]);
    Assert.NotNull(httpContext.Items["TenantContext"]);
  }

  [Fact]
  public void UserContext_ReturnsCorrectUserInformation() {
    // Arrange
    var testUserId = Guid.NewGuid().ToString();
    var httpContext = CreateHttpContextWithUser(testUserId, "test@example.com", "TestTenant");
    var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext };
    var userLogger = new Mock<ILogger<UserContext>>();
    var userContext = new UserContext(httpContextAccessor, userLogger.Object);

    // Act & Assert
    Assert.True(userContext.IsAuthenticated);
    Assert.Equal(Guid.Parse(testUserId), userContext.UserId);
    Assert.Equal("test@example.com", userContext.Email);
    Assert.Contains("Admin", userContext.Roles);
  }

  [Fact]
  public void TenantContext_ReturnsCorrectTenantInformation() {
    // Arrange
    var testUserId = Guid.NewGuid().ToString();
    var httpContext = CreateHttpContextWithUser(testUserId, "test@example.com", "TestTenant");
    httpContext.Request.Headers.Append("X-Tenant-Id", "tenant-123");
    var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext };
    var tenantLogger = new Mock<ILogger<TenantContext>>();
    var tenantContext = new TenantContext(httpContextAccessor, tenantLogger.Object);

    // Act & Assert
    Assert.Equal("TestTenant", tenantContext.TenantName);
    Assert.True(tenantContext.IsActive);
  }

  [Fact]
  public void UserContext_HandlesUnauthenticatedUser() {
    // Arrange
    var httpContext = new DefaultHttpContext();
    var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor { HttpContext = httpContext };
    var userLogger = new Mock<ILogger<UserContext>>();
    var userContext = new UserContext(httpContextAccessor, userLogger.Object);

    // Act & Assert
    Assert.False(userContext.IsAuthenticated);
    Assert.Null(userContext.UserId);
    Assert.Null(userContext.Email);
    Assert.Empty(userContext.Roles);
  }

  private static Microsoft.AspNetCore.Http.HttpContext CreateHttpContextWithUser(string userId, string email, string tenantName) {
    var claims = new[]
    {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("tenant_name", tenantName),
            new Claim("tenant_active", "true"),
        };

    var identity = new ClaimsIdentity(claims, "Test");
    var principal = new ClaimsPrincipal(identity);

    var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext {
      User = principal,
    };

    return httpContext;
  }
}

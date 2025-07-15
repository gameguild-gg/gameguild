using GameGuild.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Claims;


namespace GameGuild.Tests.Common.Middleware;

/// <summary>
/// Tests for the ContextMiddleware and related services
/// </summary>
public class ContextMiddlewareTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContextMiddleware> _logger;

    public ContextMiddlewareTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddContextServices();
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<ContextMiddleware>>();
    }

    [Fact]
    public async Task ContextMiddleware_SetsUserAndTenantContext_Successfully()
    {
        // Arrange
        var httpContext = CreateHttpContextWithUser("test-user-id", "test@example.com", "TestTenant");
        var middleware = new ContextMiddleware(
            next: (context) => Task.CompletedTask,
            logger: _logger
        );

        // Create context services with the test HTTP context
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var userContext = new UserContext(httpContextAccessor);
        var tenantContext = new TenantContext(httpContextAccessor);

        // Act
        await middleware.InvokeAsync(httpContext, userContext, tenantContext);

        // Assert
        Assert.True(userContext.IsAuthenticated);
        Assert.Equal(Guid.Parse("test-user-id"), userContext.UserId);
        Assert.Equal("test@example.com", userContext.Email);
        Assert.NotNull(httpContext.Items["UserContext"]);
        Assert.NotNull(httpContext.Items["TenantContext"]);
    }

    [Fact]
    public void UserContext_ReturnsCorrectUserInformation()
    {
        // Arrange
        var httpContext = CreateHttpContextWithUser("test-user-id", "test@example.com", "TestTenant");
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var userContext = new UserContext(httpContextAccessor);

        // Act & Assert
        Assert.True(userContext.IsAuthenticated);
        Assert.Equal(Guid.Parse("test-user-id"), userContext.UserId);
        Assert.Equal("test@example.com", userContext.Email);
        Assert.Contains("Admin", userContext.Roles);
    }

    [Fact]
    public void TenantContext_ReturnsCorrectTenantInformation()
    {
        // Arrange
        var httpContext = CreateHttpContextWithUser("test-user-id", "test@example.com", "TestTenant");
        httpContext.Request.Headers.Add("X-Tenant-Id", "tenant-123");
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var tenantContext = new TenantContext(httpContextAccessor);

        // Act & Assert
        Assert.Equal("TestTenant", tenantContext.TenantName);
        Assert.True(tenantContext.IsActive);
    }

    [Fact]
    public void UserContext_HandlesUnauthenticatedUser()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var userContext = new UserContext(httpContextAccessor);

        // Act & Assert
        Assert.False(userContext.IsAuthenticated);
        Assert.Null(userContext.UserId);
        Assert.Null(userContext.Email);
        Assert.Empty(userContext.Roles);
    }

    private static HttpContext CreateHttpContextWithUser(string userId, string email, string tenantName)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("tenant", tenantName)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };

        return httpContext;
    }
}

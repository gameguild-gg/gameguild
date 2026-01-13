using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;
using AuthorizationOptions = GameGuild.Configuration.PresentationLayer.Authorization.AuthorizationOptions;

namespace GameGuild.Identity.Authorization.UnitTests.Security;

/// <summary>
///     Tests for PermissionHandler fail-closed behavior.
///     Validates that missing/invalid context results in authorization failure.
/// </summary>
public class PermissionHandlerFailClosedTests
{
    private readonly Mock<IAuthorizationPermissionService> _mockPermissionService;
    private readonly Mock<IAuthorizationTenantContext> _mockTenantContext;
    private readonly Mock<ILogger<PermissionHandler>> _mockLogger;
    private readonly IOptions<AuthorizationOptions> _options;

    public PermissionHandlerFailClosedTests()
    {
        _mockPermissionService = new Mock<IAuthorizationPermissionService>();
        _mockTenantContext = new Mock<IAuthorizationTenantContext>();
        _mockLogger = new Mock<ILogger<PermissionHandler>>();
        _options = Options.Create(new AuthorizationOptions());
    }

    #region Fail-Closed on Missing User ID

    [Fact]
    public async Task HandleRequirement_FailsClosed_WhenUserIdMissing()
    {
        // Arrange
        var handler = new PermissionHandler(
            _mockPermissionService.Object,
            _mockTenantContext.Object,
            _options,
            _mockLogger.Object);

        // User without user ID claim
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com")
            // No user ID claim
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var requirement = new PermissionRequirement("test:permission");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert - should fail closed
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_FailsClosed_WhenUserIdIsGuidEmpty()
    {
        // Arrange
        var handler = new PermissionHandler(
            _mockPermissionService.Object,
            _mockTenantContext.Object,
            _options,
            _mockLogger.Object);

        // User with Guid.Empty as user ID
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.Empty.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var requirement = new PermissionRequirement("test:permission");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert - Guid.Empty should be rejected
        context.HasSucceeded.Should().BeFalse();
    }

    #endregion

    #region Fail-Closed on Missing Tenant

    [Fact]
    public async Task HandleRequirement_FailsClosed_WhenNoTenantContext()
    {
        // Arrange
        _mockTenantContext.Setup(t => t.HasTenant).Returns(false);
        _mockTenantContext.Setup(t => t.TenantId).Returns((Guid?)null);

        var handler = new PermissionHandler(
            _mockPermissionService.Object,
            _mockTenantContext.Object,
            _options,
            _mockLogger.Object);

        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var requirement = new PermissionRequirement("test:permission");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert - should fail closed when no tenant
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_FailsClosed_WhenTenantIdIsGuidEmpty()
    {
        // Arrange
        _mockTenantContext.Setup(t => t.HasTenant).Returns(true);
        _mockTenantContext.Setup(t => t.TenantId).Returns(Guid.Empty);

        var handler = new PermissionHandler(
            _mockPermissionService.Object,
            _mockTenantContext.Object,
            _options,
            _mockLogger.Object);

        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var requirement = new PermissionRequirement("test:permission");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert - Guid.Empty tenant should be rejected
        context.HasSucceeded.Should().BeFalse();
    }

    #endregion

    #region Success Cases

    [Fact]
    public async Task HandleRequirement_Succeeds_WithValidUserAndTenant()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext.Setup(t => t.HasTenant).Returns(true);
        _mockTenantContext.Setup(t => t.TenantId).Returns(tenantId);

        _mockPermissionService
            .Setup(p => p.HasPermissionAsync(userId, tenantId, "test:permission", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new PermissionHandler(
            _mockPermissionService.Object,
            _mockTenantContext.Object,
            _options,
            _mockLogger.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var requirement = new PermissionRequirement("test:permission");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_Fails_WhenPermissionNotGranted()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        _mockTenantContext.Setup(t => t.HasTenant).Returns(true);
        _mockTenantContext.Setup(t => t.TenantId).Returns(tenantId);

        _mockPermissionService
            .Setup(p => p.HasPermissionAsync(userId, tenantId, "test:permission", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Permission not granted

        var handler = new PermissionHandler(
            _mockPermissionService.Object,
            _mockTenantContext.Object,
            _options,
            _mockLogger.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var requirement = new PermissionRequirement("test:permission");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    #endregion

    #region Unauthenticated User

    [Fact]
    public async Task HandleRequirement_FailsClosed_WhenUnauthenticated()
    {
        // Arrange
        var handler = new PermissionHandler(
            _mockPermissionService.Object,
            _mockTenantContext.Object,
            _options,
            _mockLogger.Object);

        // Unauthenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // No authentication type

        var requirement = new PermissionRequirement("test:permission");
        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        context.HasSucceeded.Should().BeFalse();
    }

    #endregion
}

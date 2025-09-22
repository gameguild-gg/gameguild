using System.Security.Claims;
using GameGuild.Authorization;
using GameGuild.Core.Domain.Permissions;
using GameGuild;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Modules.Authorization.Unit;

/// <summary>
/// Test service provider that supports GetRequiredService extension method
/// </summary>
public class TestServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public void AddService<T>(T service)
    {
        if (service != null)
        {
            _services[typeof(T)] = service;
        }
    }

    public object? GetService(Type serviceType)
    {
        _services.TryGetValue(serviceType, out var service);
        return service;
    }
}

/// <summary>
/// Unit tests for DAC authorization attributes
/// </summary>
public class DACAuthorizationAttributeTests
{
    private readonly Mock<IPermissionService> _mockPermissionService;
    private readonly AuthorizationFilterContext _filterContext;
    private readonly ClaimsPrincipal _authenticatedUser;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public DACAuthorizationAttributeTests()
    {
        _mockPermissionService = new Mock<IPermissionService>();

        // Setup authenticated user with claims
        _authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim("tenant_id", _tenantId.ToString())
        }, "test"));

        // Setup service provider - create a manual implementation to handle GetRequiredService
        var serviceProvider = new TestServiceProvider();
        serviceProvider.AddService<IPermissionService>(_mockPermissionService.Object);

        // Setup HttpContext
        var httpContext = new DefaultHttpContext
        {
            User = _authenticatedUser,
            RequestServices = serviceProvider
        };

        // Setup AuthorizationFilterContext
        _filterContext = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            []
        );
    }

    [Fact]
    public async Task RequireTenantPermissionAttribute_WithValidPermission_ShouldAllow()
    {
        // Arrange
        var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
        _mockPermissionService.Setup(x => x.HasTenantPermissionAsync(_userId, _tenantId, PermissionType.Read))
            .ReturnsAsync(true);

        // Act
        await attribute.OnAuthorizationAsync(_filterContext);

        // Assert
        Assert.Null(_filterContext.Result); // No result means authorization passed
    }

    [Fact]
    public async Task RequireTenantPermissionAttribute_WithoutPermission_ShouldDeny()
    {
        // Arrange
        var attribute = new RequireTenantPermissionAttribute(PermissionType.Edit);
        _mockPermissionService.Setup(x => x.HasTenantPermissionAsync(_userId, _tenantId, PermissionType.Edit))
            .ReturnsAsync(false);

        // Act
        await attribute.OnAuthorizationAsync(_filterContext);

        // Assert
        Assert.IsType<PermissionDeniedResult>(_filterContext.Result);
    }

    [Fact]
    public async Task RequireTenantPermissionAttribute_WithInvalidUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
        var userWithInvalidId = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid-guid"),
            new Claim("tenant_id", _tenantId.ToString())
        }, "test"));

        _filterContext.HttpContext.User = userWithInvalidId;

        // Act
        await attribute.OnAuthorizationAsync(_filterContext);

        // Assert
        Assert.IsType<UnauthorizedResult>(_filterContext.Result);
    }

    [Fact]
    public async Task RequireTenantPermissionAttribute_WithInvalidTenantId_ShouldReturnUnauthorized()
    {
        // Arrange
        var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
        var userWithInvalidTenantId = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim("tenant_id", "invalid-guid")
        }, "test"));

        _filterContext.HttpContext.User = userWithInvalidTenantId;

        // Act
        await attribute.OnAuthorizationAsync(_filterContext);

        // Assert
        Assert.IsType<UnauthorizedResult>(_filterContext.Result);
    }

    [Fact]
    public async Task RequireTenantPermissionAttribute_FallbackToLegacyService_ShouldWork()
    {
        // Arrange - Setup a context without any special resolver to test standard IPermissionService
        var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);

        _mockPermissionService.Setup(x => x.HasTenantPermissionAsync(_userId, _tenantId, PermissionType.Read))
            .ReturnsAsync(true);

        // Act
        await attribute.OnAuthorizationAsync(_filterContext);

        // Assert
        Assert.Null(_filterContext.Result); // No result means authorization passed
        _mockPermissionService.Verify(x => x.HasTenantPermissionAsync(_userId, _tenantId, PermissionType.Read), Times.Once);
    }

    [Fact]
    public async Task RequireTenantPermissionAttribute_ExceptionDuringCheck_ShouldDeny()
    {
        // Arrange
        var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
        _mockPermissionService.Setup(x => x.HasTenantPermissionAsync(_userId, _tenantId, PermissionType.Read))
            .ThrowsAsync(new Exception("Permission check failed"));

        // Act
        await attribute.OnAuthorizationAsync(_filterContext);

        // Assert
        Assert.IsType<StatusCodeResult>(_filterContext.Result);
        var statusCodeResult = _filterContext.Result as StatusCodeResult;
        Assert.Equal(500, statusCodeResult?.StatusCode);
    }
}

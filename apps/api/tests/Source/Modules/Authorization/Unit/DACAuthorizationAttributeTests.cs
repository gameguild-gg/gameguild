using System.Security.Claims;
using GameGuild.Authorization;
using GameGuild.Core.Domain.Permissions;
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
/// Unit tests for DAC authorization attributes
/// </summary>
public class DACAuthorizationAttributeTests {
  private readonly Mock<IPermissionService> _mockPermissionService;
  private readonly Mock<IDacPermissionResolver> _mockDacResolver;
  private readonly Mock<ILogger<RequireTenantPermissionAttribute>> _mockLogger;
  private readonly Mock<IServiceProvider> _mockServiceProvider;
  private readonly AuthorizationFilterContext _filterContext;
  private readonly ClaimsPrincipal _authenticatedUser;
  private readonly Guid _userId = Guid.NewGuid();
  private readonly Guid _tenantId = Guid.NewGuid();

  public DACAuthorizationAttributeTests() {
    _mockPermissionService = new Mock<IPermissionService>();
    _mockDacResolver = new Mock<IDacPermissionResolver>();
    _mockLogger = new Mock<ILogger<RequireTenantPermissionAttribute>>();
    _mockServiceProvider = new Mock<IServiceProvider>();

    // Setup authenticated user with claims
    _authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[] {
      new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
      new Claim("tenant_id", _tenantId.ToString())
    }, "test"));

    // Setup service provider
    _mockServiceProvider.Setup(x => x.GetService(typeof(IDacPermissionResolver)))
      .Returns(_mockDacResolver.Object);
    _mockServiceProvider.Setup(x => x.GetRequiredService(typeof(IPermissionService)))
      .Returns(_mockPermissionService.Object);
    _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<RequireTenantPermissionAttribute>)))
      .Returns(_mockLogger.Object);

    // Setup HttpContext
    var httpContext = new DefaultHttpContext {
      User = _authenticatedUser,
      RequestServices = _mockServiceProvider.Object
    };

    // Setup AuthorizationFilterContext
    _filterContext = new AuthorizationFilterContext(
      new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
      new List<IFilterMetadata>()
    );
  }

  [Fact]
  public async Task RequireTenantPermissionAttribute_WithValidPermission_ShouldAllow() {
    // Arrange
    var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
    _mockDacResolver.Setup(x => x.ResolvePermissionAsync<EntityBase>(_userId, _tenantId, PermissionType.Read))
      .ReturnsAsync(new PermissionResult { IsGranted = true });

    // Act
    await attribute.OnAuthorizationAsync(_filterContext);

    // Assert
    Assert.Null(_filterContext.Result); // No result means authorization passed
  }

  [Fact]
  public async Task RequireTenantPermissionAttribute_WithoutPermission_ShouldDeny() {
    // Arrange
    var attribute = new RequireTenantPermissionAttribute(PermissionType.Edit);
    _mockDacResolver.Setup(x => x.ResolvePermissionAsync<EntityBase>(_userId, _tenantId, PermissionType.Edit))
      .ReturnsAsync(new PermissionResult { IsGranted = false });

    // Act
    await attribute.OnAuthorizationAsync(_filterContext);

    // Assert
    Assert.IsType<ForbidResult>(_filterContext.Result);
  }

  [Fact]
  public async Task RequireTenantPermissionAttribute_WithInvalidUserId_ShouldReturnUnauthorized() {
    // Arrange
    var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
    var userWithInvalidId = new ClaimsPrincipal(new ClaimsIdentity(new[] {
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
  public async Task RequireTenantPermissionAttribute_WithInvalidTenantId_ShouldReturnUnauthorized() {
    // Arrange
    var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
    var userWithInvalidTenantId = new ClaimsPrincipal(new ClaimsIdentity(new[] {
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
  public async Task RequireTenantPermissionAttribute_FallbackToLegacyService_ShouldWork() {
    // Arrange
    var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
    _mockServiceProvider.Setup(x => x.GetService(typeof(IDacPermissionResolver)))
      .Returns((IDacPermissionResolver?)null); // No DAC resolver available

    _mockPermissionService.Setup(x => x.HasTenantPermissionAsync(_userId, _tenantId, PermissionType.Read))
      .ReturnsAsync(true);

    // Act
    await attribute.OnAuthorizationAsync(_filterContext);

    // Assert
    Assert.Null(_filterContext.Result); // Authorization passed
    _mockPermissionService.Verify(x => x.HasTenantPermissionAsync(_userId, _tenantId, PermissionType.Read), Times.Once);
  }

  [Fact]
  public async Task RequireTenantPermissionAttribute_ExceptionDuringCheck_ShouldDeny() {
    // Arrange
    var attribute = new RequireTenantPermissionAttribute(PermissionType.Read);
    _mockDacResolver.Setup(x => x.ResolvePermissionAsync<EntityBase>(_userId, _tenantId, PermissionType.Read))
      .ThrowsAsync(new Exception("Permission check failed"));

    // Act
    await attribute.OnAuthorizationAsync(_filterContext);

    // Assert
    Assert.IsType<ForbidResult>(_filterContext.Result);
  }
}

/// <summary>
/// Test helper classes for permission result
/// </summary>
public class PermissionResult {
  public bool IsGranted { get; set; }
  public string? Reason { get; set; }
}

/// <summary>
/// Base entity for testing
/// </summary>
public abstract class EntityBase {
  public Guid Id { get; set; }
}

/// <summary>
/// Mock interfaces for testing
/// </summary>
public interface IDacPermissionResolver {
  Task<PermissionResult> ResolvePermissionAsync<T>(Guid userId, Guid tenantId, PermissionType permission) where T : EntityBase;
}

public interface IPermissionService {
  Task<bool> HasTenantPermissionAsync(Guid userId, Guid tenantId, PermissionType permission);
}

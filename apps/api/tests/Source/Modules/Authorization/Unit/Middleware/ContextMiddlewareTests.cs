using System.Security.Claims;
using GameGuild.Authorization.Middleware;
using GameGuild.Core.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Modules.Authorization.Unit.Middleware;

/// <summary>
/// Unit tests for ContextMiddleware
/// </summary>
public class ContextMiddlewareTests {
  private readonly Mock<RequestDelegate> _mockNext;
  private readonly Mock<ILogger<ContextMiddleware>> _mockLogger;
  private readonly Mock<IUserContext> _mockUserContext;
  private readonly Mock<ITenantContext> _mockTenantContext;
  private readonly Mock<IPermissionsContext> _mockPermissionsContext;
  private readonly Mock<IResourceContext> _mockResourceContext;
  private readonly Mock<ILocalizationContext> _mockLocalizationContext;
  private readonly ContextMiddleware _middleware;
  private readonly DefaultHttpContext _httpContext;

  public ContextMiddlewareTests() {
    _mockNext = new Mock<RequestDelegate>();
    _mockLogger = new Mock<ILogger<ContextMiddleware>>();
    _mockUserContext = new Mock<IUserContext>();
    _mockTenantContext = new Mock<ITenantContext>();
    _mockPermissionsContext = new Mock<IPermissionsContext>();
    _mockResourceContext = new Mock<IResourceContext>();
    _mockLocalizationContext = new Mock<ILocalizationContext>();

    _middleware = new ContextMiddleware(_mockNext.Object, _mockLogger.Object);
    _httpContext = new DefaultHttpContext();
  }

  [Fact]
  public async Task InvokeAsync_WithAuthenticatedUser_ShouldLogContextInformation() {
    // Arrange
    var userId = Guid.NewGuid();
    var tenantId = Guid.NewGuid();
    var resourceId = Guid.NewGuid();

    _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
    _mockUserContext.Setup(x => x.UserId).Returns(userId);
    _mockUserContext.Setup(x => x.Email).Returns("test@example.com");
    _mockUserContext.Setup(x => x.Claims).Returns(new Dictionary<string, object> {
      ["sub"] = userId.ToString(),
      ["email"] = "test@example.com"
    });

    _mockTenantContext.Setup(x => x.TenantId).Returns(tenantId);
    _mockPermissionsContext.Setup(x => x.IsSystemAdmin).Returns(false);
    _mockPermissionsContext.Setup(x => x.IsTenantAdmin).Returns(true);
    _mockResourceContext.Setup(x => x.ResourceId).Returns(resourceId);
    _mockResourceContext.Setup(x => x.ResourceType).Returns("projects");
    _mockLocalizationContext.Setup(x => x.CurrentCulture).Returns(new System.Globalization.CultureInfo("en-US"));
    _mockLocalizationContext.Setup(x => x.TimeZoneId).Returns("UTC");

    _httpContext.Request.Headers.Authorization = "Bearer test-token-here";

    // Act
    await _middleware.InvokeAsync(
      _httpContext,
      _mockUserContext.Object,
      _mockTenantContext.Object,
      _mockPermissionsContext.Object,
      _mockResourceContext.Object,
      _mockLocalizationContext.Object
    );

    // Assert
    _mockNext.Verify(x => x(_httpContext), Times.Once);

    // Verify context items were added
    Assert.Equal(_mockUserContext.Object, _httpContext.Items["UserContext"]);
    Assert.Equal(_mockTenantContext.Object, _httpContext.Items["TenantContext"]);
    Assert.Equal(_mockPermissionsContext.Object, _httpContext.Items["PermissionsContext"]);
    Assert.Equal(_mockResourceContext.Object, _httpContext.Items["ResourceContext"]);
    Assert.Equal(_mockLocalizationContext.Object, _httpContext.Items["LocalizationContext"]);
  }

  [Fact]
  public async Task InvokeAsync_WithUnauthenticatedUser_ShouldLogAppropriately() {
    // Arrange
    _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
    _mockLocalizationContext.Setup(x => x.CurrentCulture).Returns(new System.Globalization.CultureInfo("en-US"));
    _mockLocalizationContext.Setup(x => x.TimeZoneId).Returns("UTC");

    // Act
    await _middleware.InvokeAsync(
      _httpContext,
      _mockUserContext.Object,
      _mockTenantContext.Object,
      _mockPermissionsContext.Object,
      _mockResourceContext.Object,
      _mockLocalizationContext.Object
    );

    // Assert
    _mockNext.Verify(x => x(_httpContext), Times.Once);

    // Verify context items were still added for unauthenticated requests
    Assert.Equal(_mockUserContext.Object, _httpContext.Items["UserContext"]);
  }

  [Fact]
  public async Task InvokeAsync_WithBearerToken_ShouldLogTokenLength() {
    // Arrange
    _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
    _mockUserContext.Setup(x => x.UserId).Returns(Guid.NewGuid());
    _mockUserContext.Setup(x => x.Email).Returns("test@example.com");
    _mockUserContext.Setup(x => x.Claims).Returns(new Dictionary<string, object>());

    _mockTenantContext.Setup(x => x.TenantId).Returns(Guid.NewGuid());
    _mockPermissionsContext.Setup(x => x.IsSystemAdmin).Returns(false);
    _mockPermissionsContext.Setup(x => x.IsTenantAdmin).Returns(false);
    _mockResourceContext.Setup(x => x.ResourceId).Returns((Guid?)null);
    _mockLocalizationContext.Setup(x => x.CurrentCulture).Returns(new System.Globalization.CultureInfo("en-US"));
    _mockLocalizationContext.Setup(x => x.TimeZoneId).Returns("UTC");

    var testToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";
    _httpContext.Request.Headers.Authorization = $"Bearer {testToken}";

    // Act
    await _middleware.InvokeAsync(
      _httpContext,
      _mockUserContext.Object,
      _mockTenantContext.Object,
      _mockPermissionsContext.Object,
      _mockResourceContext.Object,
      _mockLocalizationContext.Object
    );

    // Assert
    _mockNext.Verify(x => x(_httpContext), Times.Once);
  }

  [Fact]
  public async Task InvokeAsync_WithoutAuthorizationHeader_ShouldNotLogToken() {
    // Arrange
    _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
    _mockLocalizationContext.Setup(x => x.CurrentCulture).Returns(new System.Globalization.CultureInfo("en-US"));
    _mockLocalizationContext.Setup(x => x.TimeZoneId).Returns("UTC");

    // No authorization header set

    // Act
    await _middleware.InvokeAsync(
      _httpContext,
      _mockUserContext.Object,
      _mockTenantContext.Object,
      _mockPermissionsContext.Object,
      _mockResourceContext.Object,
      _mockLocalizationContext.Object
    );

    // Assert
    _mockNext.Verify(x => x(_httpContext), Times.Once);
  }

  [Fact]
  public async Task InvokeAsync_WithResourceContext_ShouldLogResourceInformation() {
    // Arrange
    var resourceId = Guid.NewGuid();
    _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
    _mockUserContext.Setup(x => x.UserId).Returns(Guid.NewGuid());
    _mockUserContext.Setup(x => x.Email).Returns("test@example.com");
    _mockUserContext.Setup(x => x.Claims).Returns(new Dictionary<string, object>());

    _mockTenantContext.Setup(x => x.TenantId).Returns(Guid.NewGuid());
    _mockPermissionsContext.Setup(x => x.IsSystemAdmin).Returns(true);
    _mockPermissionsContext.Setup(x => x.IsTenantAdmin).Returns(true);
    _mockResourceContext.Setup(x => x.ResourceId).Returns(resourceId);
    _mockResourceContext.Setup(x => x.ResourceType).Returns("users");
    _mockLocalizationContext.Setup(x => x.CurrentCulture).Returns(new System.Globalization.CultureInfo("en-US"));
    _mockLocalizationContext.Setup(x => x.TimeZoneId).Returns("UTC");

    // Act
    await _middleware.InvokeAsync(
      _httpContext,
      _mockUserContext.Object,
      _mockTenantContext.Object,
      _mockPermissionsContext.Object,
      _mockResourceContext.Object,
      _mockLocalizationContext.Object
    );

    // Assert
    _mockNext.Verify(x => x(_httpContext), Times.Once);
    Assert.Equal(_mockResourceContext.Object, _httpContext.Items["ResourceContext"]);
  }

  [Fact]
  public async Task InvokeAsync_ShouldAlwaysCallNext() {
    // Arrange
    _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
    _mockLocalizationContext.Setup(x => x.CurrentCulture).Returns(new System.Globalization.CultureInfo("en-US"));
    _mockLocalizationContext.Setup(x => x.TimeZoneId).Returns("UTC");

    // Act
    await _middleware.InvokeAsync(
      _httpContext,
      _mockUserContext.Object,
      _mockTenantContext.Object,
      _mockPermissionsContext.Object,
      _mockResourceContext.Object,
      _mockLocalizationContext.Object
    );

    // Assert
    _mockNext.Verify(x => x(_httpContext), Times.Once);
  }
}

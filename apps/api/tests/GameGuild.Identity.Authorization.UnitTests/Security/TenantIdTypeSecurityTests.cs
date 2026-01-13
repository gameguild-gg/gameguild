using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Security;

/// <summary>
///     Tests for type-safe TenantId handling and Guid.Empty rejection.
///     Validates Attack 5 mitigations (Type Confusion in Tenant ID).
/// </summary>
public class TenantIdTypeSecurityTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<HttpAuthorizationTenantContext>> _mockLogger;

    public TenantIdTypeSecurityTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<HttpAuthorizationTenantContext>>();
    }

    #region Guid.Empty Rejection

    [Fact]
    public void HttpAuthorizationTenantContext_RejectsGuidEmpty()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = Guid.Empty;

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert - Guid.Empty should be rejected as invalid
        tenantId.Should().BeNull();
        tenantContext.HasTenant.Should().BeFalse();
    }

    [Fact]
    public void HttpAuthorizationTenantContext_RejectsEmptyGuidString()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = "00000000-0000-0000-0000-000000000000";

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert - Guid.Empty string should be rejected
        tenantId.Should().BeNull();
        tenantContext.HasTenant.Should().BeFalse();
    }

    [Fact]
    public void HttpAuthorizationTenantContext_AcceptsValidGuid()
    {
        // Arrange
        var validTenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = validTenantId;

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert
        tenantId.Should().Be(validTenantId);
        tenantContext.HasTenant.Should().BeTrue();
    }

    [Fact]
    public void HttpAuthorizationTenantContext_AcceptsValidGuidString()
    {
        // Arrange
        var validTenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = validTenantId.ToString();

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert
        tenantId.Should().Be(validTenantId);
        tenantContext.HasTenant.Should().BeTrue();
    }

    #endregion

    #region Fallback Behavior

    [Fact]
    public void HttpAuthorizationTenantContext_FallsBackToTenantMiddlewareKey()
    {
        // Arrange
        var validTenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        // Use TenantMiddleware key instead of AuthorizationTenantId
        httpContext.Items["TenantId"] = validTenantId;

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert
        tenantId.Should().Be(validTenantId);
    }

    [Fact]
    public void HttpAuthorizationTenantContext_ReturnsNull_WhenNoContext()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert
        tenantId.Should().BeNull();
        tenantContext.HasTenant.Should().BeFalse();
    }

    [Fact]
    public void HttpAuthorizationTenantContext_ReturnsNull_WhenNoTenantKey()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        // No tenant ID set

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert
        tenantId.Should().BeNull();
        tenantContext.HasTenant.Should().BeFalse();
    }

    #endregion

    #region Invalid Input Handling

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("xyz-abc-def")]
    public void HttpAuthorizationTenantContext_RejectsInvalidGuidStrings(string invalidTenantId)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = invalidTenantId;

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert - invalid strings should be rejected
        tenantId.Should().BeNull();
        tenantContext.HasTenant.Should().BeFalse();
    }

    [Fact]
    public void HttpAuthorizationTenantContext_RejectsNonGuidTypes()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Items["TenantId"] = 12345; // Integer instead of Guid

        _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var tenantContext = new HttpAuthorizationTenantContext(
            _mockHttpContextAccessor.Object,
            _mockLogger.Object);

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert - non-Guid types should be rejected
        tenantId.Should().BeNull();
    }

    #endregion
}

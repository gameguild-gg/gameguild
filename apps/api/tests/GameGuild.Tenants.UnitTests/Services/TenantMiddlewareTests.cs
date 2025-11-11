using GameGuild.Modules.Tenants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Services;

/// <summary>
/// Unit tests for TenantMiddleware
/// </summary>
public class TenantMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<TenantMiddleware>> _mockLogger;
    private readonly Mock<ITenantService> _mockTenantService;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly TenantMiddleware _middleware;

    public TenantMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<TenantMiddleware>>();
        _mockTenantService = new Mock<ITenantService>();
        _mockTenantContext = new Mock<ITenantContext>();

        _middleware = new TenantMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Resolve_Tenant_And_Continue_Pipeline()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant", Slug = "test-tenant" };

        SetupTenantResolution(tenant);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert
        _mockTenantContext.Verify(c => c.SetCurrentTenant(tenant), Times.Once);
        _mockNext.Verify(n => n(httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_Null_Tenant_When_Not_Found()
    {
        // Arrange
        var httpContext = CreateHttpContext();

        SetupTenantResolution(null);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert
        _mockTenantContext.Verify(c => c.SetCurrentTenant(null), Times.Once);
        _mockNext.Verify(n => n(httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Exception_And_Fallback_To_Default_Tenant()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var defaultTenant = new Tenant { Id = Guid.NewGuid(), Name = "Default", IsDefault = true };

        // Setup subdomain resolution to throw exception
        httpContext.Request.Host = new HostString("invalid.example.com");
        _ = _mockTenantService.Setup(s => s.GetTenantBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        _ = _mockTenantService.Setup(s => s.GetDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultTenant);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert
        _mockTenantContext.Verify(c => c.SetCurrentTenant(defaultTenant), Times.Once);
        _mockNext.Verify(n => n(httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Resolve_Tenant_From_Subdomain()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString("tenant1.example.com");

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant 1", Slug = "tenant1" };

        _ = _mockTenantService.Setup(s => s.GetTenantBySlugAsync("tenant1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert
        _mockTenantService.Verify(s => s.GetTenantBySlugAsync("tenant1", It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantContext.Verify(c => c.SetCurrentTenant(tenant), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Resolve_Tenant_From_Header()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["X-Tenant-Slug"] = "tenant-from-header";

        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Header Tenant", Slug = "tenant-from-header" };

        // Setup subdomain check to return null (no subdomain in this test)
        _ = _mockTenantService.Setup(s => s.GetTenantBySlugAsync("example", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _ = _mockTenantService.Setup(s => s.GetTenantBySlugAsync("tenant-from-header", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert - middleware will try subdomain (example), then header (tenant-from-header), then default
        _mockTenantService.Verify(s => s.GetTenantBySlugAsync("example", It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantService.Verify(s => s.GetTenantBySlugAsync("tenant-from-header", It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantContext.Verify(c => c.SetCurrentTenant(tenant), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Prioritize_Header_Over_Subdomain()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString("subdomain.example.com");
        httpContext.Request.Headers["X-Tenant-Slug"] = "header-tenant";

        var headerTenant = new Tenant { Id = Guid.NewGuid(), Name = "Header Tenant", Slug = "header-tenant" };

        // Setup subdomain check to return null first
        _ = _mockTenantService.Setup(s => s.GetTenantBySlugAsync("subdomain", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        _ = _mockTenantService.Setup(s => s.GetTenantBySlugAsync("header-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(headerTenant);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert - middleware checks subdomain first, then header when subdomain fails
        _mockTenantService.Verify(s => s.GetTenantBySlugAsync("subdomain", It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantService.Verify(s => s.GetTenantBySlugAsync("header-tenant", It.IsAny<CancellationToken>()), Times.Once);
        _mockTenantContext.Verify(c => c.SetCurrentTenant(headerTenant), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Empty_Host()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString();

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert
        _mockTenantContext.Verify(c => c.SetCurrentTenant(null), Times.Once);
        _mockNext.Verify(n => n(httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Invalid_Subdomain_Format()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString("not-a-subdomain");

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert
        _mockTenantContext.Verify(c => c.SetCurrentTenant(null), Times.Once);
        _mockNext.Verify(n => n(httpContext), Times.Once);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("api.example.com")]
    [InlineData("www.example.com")]
    public async Task InvokeAsync_Should_Handle_Known_Non_Tenant_Hosts(string host)
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Host = new HostString(host);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTenantService.Object, _mockTenantContext.Object);

        // Assert
        _mockTenantContext.Verify(c => c.SetCurrentTenant(null), Times.Once);
        _mockNext.Verify(n => n(httpContext), Times.Once);
    }

    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("example.com");
        context.Request.Headers["X-Tenant"] = StringValues.Empty;
        return context;
    }

    private void SetupTenantResolution(Tenant? tenant)
    {
        _ = _mockTenantService.Setup(s => s.GetTenantBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
    }
}
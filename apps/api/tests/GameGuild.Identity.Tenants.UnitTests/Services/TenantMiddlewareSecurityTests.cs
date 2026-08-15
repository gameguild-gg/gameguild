using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Services;

/// <summary>
///     Unit tests for TenantMiddleware tenant membership validation
/// </summary>
public class TenantMiddlewareSecurityTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ILogger<TenantMiddleware>> _loggerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ITenantDomainsRepository> _domainRepoMock;
    private readonly Mock<ITenantMemberRepository> _memberRepoMock;
    private readonly TenantMiddleware _middleware;

    public TenantMiddlewareSecurityTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _loggerMock = new Mock<ILogger<TenantMiddleware>>();
        _mediatorMock = new Mock<IMediator>();
        _domainRepoMock = new Mock<ITenantDomainsRepository>();
        _memberRepoMock = new Mock<ITenantMemberRepository>();
        _middleware = new TenantMiddleware(_nextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Should_AllowAccess_WhenAuthenticatedUserIsMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test", IsActive = true };
        var membership = new TenantMember { UserId = userId, TenantId = tenantId, IsActive = true };

        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: userId,
            tenantIdHeader: tenantId.ToString());

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _memberRepoMock
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        // Act
        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Items[HttpContextKeys.CurrentTenant].Should().Be(tenant);
        context.Items[HttpContextKeys.AuthorizationTenantId].Should().Be(tenantId);
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task Should_AllowSystemAdmin_WhenNotMemberOfResolvedTenant()
    {
        // A platform-level administrator must be able to operate across tenants even
        // when they do not hold a tenant-local membership.
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Other tenant", Slug = "other", IsActive = true };
        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: userId,
            tenantIdHeader: tenantId.ToString(),
            roles: ["SystemAdmin"]);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        context.Response.StatusCode.Should().Be(200);
        _nextMock.Verify(n => n(context), Times.Once);
        _memberRepoMock.Verify(
            r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_AllowSystemAdminToSelectAnExplicitTargetTenantWhileKeepingTheBaseTenantClaim()
    {
        var userId = Guid.NewGuid();
        var baseTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var targetTenant = new Tenant
        {
            Id = targetTenantId,
            Name = "Target tenant",
            Slug = "target",
            IsActive = true,
        };
        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: userId,
            tenantIdHeader: targetTenantId.ToString(),
            roles: ["SystemAdmin"],
            authenticatedTenantId: baseTenantId);
        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetTenantByIdQuery>(q => q.TenantId == targetTenantId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetTenant);

        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context.Items[HttpContextKeys.AuthorizationTenantId].Should().Be(targetTenantId);
        _nextMock.Verify(next => next(context), Times.Once);
        _memberRepoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Should_NotTreatTenantAdminAsSystemAdmin_WhenNotMemberOfResolvedTenant()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Other tenant", Slug = "other", IsActive = true };
        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: userId,
            tenantIdHeader: tenantId.ToString(),
            roles: ["Admin"]);

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _memberRepoMock
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _nextMock.Verify(n => n(context), Times.Never);
        _memberRepoMock.Verify(
            r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Return403_WhenAuthenticatedUserNotMember()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test", IsActive = true };

        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: userId,
            tenantIdHeader: tenantId.ToString());

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _memberRepoMock
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null); // Not a member

        // Act
        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(403);
        context.Items.Should().NotContainKey(HttpContextKeys.CurrentTenant);
        _nextMock.Verify(n => n(context), Times.Never);

        // Verify security log
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempted to access tenant")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Return403_WhenUserHasInactiveMembership()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test", IsActive = true };
        var inactiveMembership = new TenantMember
        {
            UserId = userId,
            TenantId = tenantId,
            IsActive = false // Inactive membership
        };

        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: userId,
            tenantIdHeader: tenantId.ToString());

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _memberRepoMock
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveMembership);

        // Act
        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(403);
        _nextMock.Verify(n => n(context), Times.Never);
    }

    [Fact]
    public async Task Should_SkipValidation_WhenUserIsAnonymous()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test", IsActive = true };

        var context = CreateHttpContext(
            isAuthenticated: false,
            userId: null,
            tenantIdHeader: tenantId.ToString());

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Items[HttpContextKeys.CurrentTenant].Should().Be(tenant);
        _nextMock.Verify(n => n(context), Times.Once);
        
        // Membership check should NOT be called for anonymous users
        _memberRepoMock.Verify(
            r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_FailClosed_WhenMembershipCheckThrows()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test", IsActive = true };

        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: userId,
            tenantIdHeader: tenantId.ToString());

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _memberRepoMock
            .Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(403, "should fail-closed on errors");
        _nextMock.Verify(n => n(context), Times.Never);

        // Verify error is logged
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to validate tenant membership")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_SkipValidation_WhenPathIsBypassed()
    {
        // Arrange
        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: Guid.NewGuid(),
            tenantIdHeader: null,
            path: "/health");

        // Act
        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        _nextMock.Verify(n => n(context), Times.Once);
        
        // No tenant resolution or membership check
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<GetTenantByIdQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _memberRepoMock.Verify(
            r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_SkipValidation_WhenNoTenantResolved()
    {
        // Arrange
        var context = CreateHttpContext(
            isAuthenticated: true,
            userId: Guid.NewGuid(),
            tenantIdHeader: null); // No tenant hint

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDefaultTenantQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null); // No default tenant

        // Act
        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        context.Items.Should().NotContainKey(HttpContextKeys.CurrentTenant);
        _nextMock.Verify(n => n(context), Times.Once);
        
        // No membership check when no tenant resolved
        _memberRepoMock.Verify(
            r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("invalid-guid")]
    [InlineData("")]
    [InlineData("not-a-user-id")]
    public async Task Should_TreatAsAnonymous_WhenUserIdClaimIsInvalid(string invalidUserId)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test", IsActive = true };

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.Request.Headers["X-Tenant-Id"] = tenantId.ToString();
        
        // Set authenticated but with invalid user ID claim
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, invalidUserId)
        };
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        _mediatorMock
            .Setup(m => m.Send(It.Is<GetTenantByIdQuery>(q => q.TenantId == tenantId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        await _middleware.InvokeAsync(
            context,
            _mediatorMock.Object,
            _domainRepoMock.Object,
            _memberRepoMock.Object);

        // Assert
        context.Response.StatusCode.Should().Be(200);
        
        // Should NOT call membership check (treated as anonymous)
        _memberRepoMock.Verify(
            r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static DefaultHttpContext CreateHttpContext(
        bool isAuthenticated,
        Guid? userId,
        string? tenantIdHeader,
        string path = "/api/test",
        IEnumerable<string>? roles = null,
        Guid? authenticatedTenantId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        if (!string.IsNullOrEmpty(tenantIdHeader))
        {
            context.Request.Headers["X-Tenant-Id"] = tenantIdHeader;
        }

        if (isAuthenticated && userId.HasValue)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString())
            };
            if (authenticatedTenantId.HasValue)
            {
                claims.Add(new Claim(TenantResolver.TenantIdClaimType, authenticatedTenantId.Value.ToString()));
            }
            claims.AddRange((roles ?? []).Select(role => new Claim(ClaimTypes.Role, role)));
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }
        else
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return context;
    }
}

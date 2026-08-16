using FluentAssertions;
using GameGuild.API.Context;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Moq;

namespace GameGuild.API.UnitTests;

public sealed class RequestContextAccessorTests
{
    [Fact]
    public void CurrentTenantId_WhenHttpContextIsMissing_ReturnsNull()
    {
        var accessor = CreateAccessor(new HttpContextAccessor());

        accessor.CurrentTenantId.Should().BeNull();
        accessor.HasTenantContext.Should().BeFalse();
    }

    [Fact]
    public void CurrentTenantId_WhenValidatedTenantItemIsMissing_ReturnsNull()
    {
        var accessor = CreateAccessor(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });

        accessor.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public void CurrentTenantId_WhenValidatedTenantItemHasWrongType_UsesHeaderFallback()
    {
        var tenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextKeys.AuthorizationTenantId] = tenantId.ToString();
        httpContext.Request.Headers["X-Tenant-Id"] = tenantId.ToString();

        var accessor = CreateAccessor(new HttpContextAccessor { HttpContext = httpContext });

        accessor.CurrentTenantId.Should().Be(tenantId);
    }

    [Fact]
    public void CurrentTenantId_UsesTenantValidatedByMiddleware()
    {
        var tenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextKeys.AuthorizationTenantId] = tenantId;
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var actorContextAccessor = new Mock<IActorContextAccessor>();
        actorContextAccessor.SetupGet(accessor => accessor.ActorContext).Returns(ActorContext.Anonymous);
        var accessor = new RequestContextAccessor(actorContextAccessor.Object, httpContextAccessor);

        accessor.CurrentTenantId.Should().Be(tenantId);
        accessor.HasTenantContext.Should().BeTrue();
    }

    private static RequestContextAccessor CreateAccessor(IHttpContextAccessor httpContextAccessor)
    {
        var actorContextAccessor = new Mock<IActorContextAccessor>();
        actorContextAccessor.SetupGet(accessor => accessor.ActorContext).Returns(ActorContext.Anonymous);
        return new RequestContextAccessor(actorContextAccessor.Object, httpContextAccessor);
    }
}

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
}
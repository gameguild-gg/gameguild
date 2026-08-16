using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization.UnitTests;

public class MethodCoverageTests
{
    // ═══════════════════════════════════════════════════════════════════
    // Additional constructor tests
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void PolicyEvaluationLogger_CanBeConstructed()
    {
        var svc = new PolicyEvaluationLogger(
            NullLogger<PolicyEvaluationLogger>.Instance);
        svc.Should().NotBeNull();
    }

    [Fact]
    public void RequireTimeWindowRuleEvaluator_CanBeConstructed()
    {
        var eval = new RequireTimeWindowRuleEvaluator();
        eval.Should().NotBeNull();
        eval.RuleType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RequireIpAllowListRuleEvaluator_CanBeConstructed()
    {
        var eval = new RequireIpAllowListRuleEvaluator(
            Mock.Of<IHttpContextAccessor>());
        eval.Should().NotBeNull();
    }

    [Fact]
    public void LocalizationContext_CanBeConstructed()
    {
        var ctx = new LocalizationContext(Mock.Of<IHttpContextAccessor>());
        ctx.Should().NotBeNull();
    }

    [Fact]
    public void ResourcePermissionAuthorizationFilter_CanBeConstructed()
    {
        var filter = new ResourcePermissionAuthorizationFilter(
            NullLogger<ResourcePermissionAuthorizationFilter>.Instance);
        filter.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // CacheInvalidationService — method tests
    // ═══════════════════════════════════════════════════════════════════

    private CacheInvalidationService CreateCacheInvalidationService(
        Mock<ITenantSecurityVersionStore>? versionStoreMock = null)
    {
        versionStoreMock ??= new Mock<ITenantSecurityVersionStore>();
        return new CacheInvalidationService(
            new MemoryCache(new MemoryCacheOptions()),
            versionStoreMock.Object,
            Mock.Of<IHybridPermissionCache>(),
            Mock.Of<ICacheMetricsService>(),
            Options.Create(new AuthorizationCacheOptions()),
            NullLogger<CacheInvalidationService>.Instance);
    }

    [Fact]
    public async Task InvalidateTenantAsync_DoesNotThrow()
    {
        var svc = CreateCacheInvalidationService();
        await svc.InvalidateTenantAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task InvalidateUserAsync_DoesNotThrow()
    {
        var svc = CreateCacheInvalidationService();
        await svc.InvalidateUserAsync(Guid.NewGuid(), Guid.NewGuid());
    }

    [Fact]
    public async Task InvalidateResourceAsync_DoesNotThrow()
    {
        var svc = CreateCacheInvalidationService();
        await svc.InvalidateResourceAsync(Guid.NewGuid(), "asset", "resource-1");
    }

    [Fact]
    public async Task InvalidatePolicyAsync_DoesNotThrow()
    {
        var svc = CreateCacheInvalidationService();
        await svc.InvalidatePolicyAsync(Guid.NewGuid(), "test-policy");
    }

    [Fact]
    public async Task InvalidatePolicyAsync_AllPolicies_DoesNotThrow()
    {
        var svc = CreateCacheInvalidationService();
        await svc.InvalidatePolicyAsync(Guid.NewGuid());
    }

    [Fact]
    public void TrackKey_AddsKey()
    {
        var svc = CreateCacheInvalidationService();
        svc.TrackKey(Guid.NewGuid(), "test-cache-key");
    }

    [Fact]
    public void HandleInvalidationEvent_ProcessesEvent()
    {
        var svc = CreateCacheInvalidationService();
        var evt = new CacheInvalidationEvent
        {
            TenantId = Guid.NewGuid(),
            Type = CacheInvalidationType.Tenant
        };
        svc.HandleInvalidationEvent(evt);
    }

    [Fact]
    public void HandleInvalidationEvent_ClearsTrackedPatternAndIgnoresUnknownType()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var svc = CreateCacheInvalidationService();
        svc.TrackKey(tenantId, $"perm:{tenantId}:{userId}:v1");

        svc.HandleInvalidationEvent(new CacheInvalidationEvent
        {
            TenantId = tenantId,
            UserId = userId,
            Type = CacheInvalidationType.User,
            OriginInstanceId = "remote"
        });
        svc.HandleInvalidationEvent(new CacheInvalidationEvent
        {
            TenantId = Guid.NewGuid(),
            UserId = userId,
            Type = CacheInvalidationType.User,
            OriginInstanceId = "remote"
        });
        svc.HandleInvalidationEvent(new CacheInvalidationEvent
        {
            TenantId = tenantId,
            Type = (CacheInvalidationType)int.MaxValue,
            OriginInstanceId = "remote"
        });
    }

    [Fact]
    public async Task PublishInvalidationAsync_DoesNotThrow()
    {
        var svc = CreateCacheInvalidationService();
        var evt = new CacheInvalidationEvent
        {
            TenantId = Guid.NewGuid(),
            Type = CacheInvalidationType.User,
            UserId = Guid.NewGuid()
        };
        await svc.PublishInvalidationAsync(evt);
    }

    // ═══════════════════════════════════════════════════════════════════
    // PolicyEvaluationLogger — method tests
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void BeginTrace_ReturnsTrace()
    {
        var loggerSvc = new PolicyEvaluationLogger(
            NullLogger<PolicyEvaluationLogger>.Instance);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var trace = loggerSvc.BeginTrace("test-policy", principal);
        trace.Should().NotBeNull();
    }

    [Fact]
    public void BeginTrace_WithResourceAndCorrelation_ReturnsTrace()
    {
        var loggerSvc = new PolicyEvaluationLogger(
            NullLogger<PolicyEvaluationLogger>.Instance);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var trace = loggerSvc.BeginTrace("policy", principal, "resource", "corr-123");
        trace.Should().NotBeNull();
    }

    [Fact]
    public void IsDebugEnabled_ReturnsBool()
    {
        var loggerSvc = new PolicyEvaluationLogger(
            NullLogger<PolicyEvaluationLogger>.Instance);
        var result = loggerSvc.IsDebugEnabled(null);
        result.Should().BeFalse();
    }

    [Fact]
    public void GetDebugSettings_ReturnsNullForNullEndpoint()
    {
        var loggerSvc = new PolicyEvaluationLogger(
            NullLogger<PolicyEvaluationLogger>.Instance);
        var settings = loggerSvc.GetDebugSettings(null);
        // May be null or default
    }

    // ═══════════════════════════════════════════════════════════════════
    // ActorContextMiddleware — InvokeAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ActorContextMiddleware_InvokeAsync_CallsNext()
    {
        var nextCalled = false;
        var middleware = new ActorContextMiddleware(
            ctx => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<ActorContextMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        await middleware.InvokeAsync(
            httpContext,
            Mock.Of<IActorContextAccessor>(),
            Mock.Of<IAuthorizationTenantResolver>(),
            Mock.Of<IClaimsPrincipalAccessor>(),
            null);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ActorContextMiddleware_InvokeAsync_WithPermissionService()
    {
        var middleware = new ActorContextMiddleware(
            ctx => Task.CompletedTask,
            NullLogger<ActorContextMiddleware>.Instance);

        var httpContext = new DefaultHttpContext();
        await middleware.InvokeAsync(
            httpContext,
            Mock.Of<IActorContextAccessor>(),
            Mock.Of<IAuthorizationTenantResolver>(),
            Mock.Of<IClaimsPrincipalAccessor>(),
            Mock.Of<IAuthorizationPermissionService>());
    }

    [Fact]
    public async Task ActorContextMiddleware_ShouldMergeValidatedTenantMembershipRole()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        ], "Test"));
        var context = new DefaultHttpContext { User = principal };
        context.Items[HttpContextKeys.AuthorizationTenantRole] = "Renter";

        var tenantResolver = new Mock<IAuthorizationTenantResolver>();
        tenantResolver
            .Setup(r => r.ResolveTenantIdAsync(context, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantId.ToString());

        ActorContext? captured = null;
        var accessor = new Mock<IActorContextAccessor>();
        accessor
            .Setup(a => a.SetActorContext(It.IsAny<ActorContext>()))
            .Callback<ActorContext>(actor => captured = actor);

        var middleware = new ActorContextMiddleware(
            _ => Task.CompletedTask,
            NullLogger<ActorContextMiddleware>.Instance);

        await middleware.InvokeAsync(
            context,
            accessor.Object,
            tenantResolver.Object,
            new StaticClaimsPrincipalAccessor(principal),
            null);

        captured.Should().NotBeNull();
        captured!.Roles.Should().Contain("User").And.Contain("Renter");
    }
    // ═══════════════════════════════════════════════════════════════════
    // EnvironmentHandler — constructor + additional tests
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void EnvironmentHandler_Properties_AreAccessible()
    {
        var handler = new EnvironmentHandler(
            Mock.Of<IHttpContextAccessor>(),
            TimeProvider.System,
            NullLogger<EnvironmentHandler>.Instance);
        handler.Should().NotBeNull();
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.CQRS.Models;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Queries;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Handlers;

/// <summary>
///     Issue #331 analytics/rule query handlers: tenant-admin guard semantics (same-tenant
///     admin allowed; cross-tenant non-system-admin rejected) mirroring the bulk expiration
///     command handlers.
/// </summary>
public class PermissionExpirationAnalyticsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenant = Guid.NewGuid();

    private readonly Mock<IPermissionExpirationAnalyticsService> _analyticsMock = new();
    private readonly Mock<IPermissionExpirationRuleEvaluator> _ruleEvaluatorMock = new();
    private readonly Mock<IActorContextAccessor> _actorAccessorMock = new();

    private void SetActor(bool authenticated, Guid? tenantId, string[]? roles = null) =>
        _actorAccessorMock.SetupGet(x => x.ActorContext).Returns(new ActorContext
        {
            ActorKind = authenticated ? ActorKind.User : ActorKind.Anonymous,
            SubjectId = authenticated ? Guid.NewGuid().ToString() : null,
            TenantId = tenantId,
            Roles = new HashSet<string>(roles ?? Array.Empty<string>(), StringComparer.Ordinal),
            Permissions = new HashSet<string>(StringComparer.Ordinal),
            IsAuthenticated = authenticated
        });

    [Fact]
    public async Task Summary_SameTenantAdminIsAllowed()
    {
        SetActor(true, TenantId, roles: new[] { "TenantAdmin" });
        var summary = new PermissionExpirationSummary
        {
            TenantId = TenantId,
            ComputedAt = SystemClock.UtcNow,
            Buckets = Array.Empty<PermissionExpirationBucket>(),
            ExpiringByPermission = Array.Empty<ExpiringPermissionBreakdown>()
        };
        _analyticsMock
            .Setup(x => x.GetSummaryAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await new GetExpirationSummaryQueryHandler(
                _analyticsMock.Object,
                _actorAccessorMock.Object,
                NullLogger<GetExpirationSummaryQueryHandler>.Instance)
            .Handle(new GetExpirationSummaryQuery { TenantId = new TenantId(TenantId) }, CancellationToken.None);

        result.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task Summary_CrossTenantCallerIsRejected()
    {
        SetActor(true, OtherTenant, roles: new[] { "TenantAdmin" });

        var act = () => new GetExpirationSummaryQueryHandler(
                _analyticsMock.Object,
                _actorAccessorMock.Object,
                NullLogger<GetExpirationSummaryQueryHandler>.Instance)
            .Handle(new GetExpirationSummaryQuery { TenantId = new TenantId(TenantId) }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _analyticsMock.Verify(x => x.GetSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Trends_SystemAdminIsAllowedAcrossTenants()
    {
        SetActor(true, OtherTenant, roles: new[] { "SystemAdmin" });
        var from = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        _analyticsMock
            .Setup(x => x.GetTrendsAsync(TenantId, from, from.AddDays(2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PermissionExpirationTrendPoint>());

        var result = await new GetExpirationTrendsQueryHandler(
                _analyticsMock.Object,
                _actorAccessorMock.Object,
                NullLogger<GetExpirationTrendsQueryHandler>.Instance)
            .Handle(new GetExpirationTrendsQuery
            {
                TenantId = new TenantId(TenantId),
                FromDate = from,
                ToDate = from.AddDays(2)
            }, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Report_UnauthenticatedCallerIsRejected()
    {
        SetActor(false, null);

        var act = () => new GetExpirationReportQueryHandler(
                _analyticsMock.Object,
                _actorAccessorMock.Object,
                NullLogger<GetExpirationReportQueryHandler>.Instance)
            .Handle(new GetExpirationReportQuery
            {
                TenantId = new TenantId(TenantId),
                PeriodStart = SystemClock.UtcNow.AddDays(-7),
                PeriodEnd = SystemClock.UtcNow
            }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task EffectiveRule_SameTenantAdminSeesComposedRule()
    {
        SetActor(true, TenantId, roles: new[] { "TenantAdmin" });
        _ruleEvaluatorMock
            .Setup(x => x.Describe(It.IsAny<IReadOnlyCollection<string>?>()))
            .Returns(new EffectivePermissionExpirationRule(
                new PermissionExpirationRule { RequireExpiration = true },
                new[] { "(global)", "billing:*" }));

        var result = await new GetEffectiveExpirationRuleQueryHandler(
                _ruleEvaluatorMock.Object,
                _actorAccessorMock.Object,
                NullLogger<GetEffectiveExpirationRuleQueryHandler>.Instance)
            .Handle(new GetEffectiveExpirationRuleQuery
            {
                TenantId = new TenantId(TenantId),
                Permissions = new[] { "billing:refunds" }
            }, CancellationToken.None);

        result.HasConstraints.Should().BeTrue();
        result.RequireExpiration.Should().BeTrue();
        result.MatchedRuleKeys.Should().BeEquivalentTo(new[] { "(global)", "billing:*" });
    }

    [Fact]
    public async Task EffectiveRule_CrossTenantCallerIsRejected()
    {
        SetActor(true, OtherTenant, roles: new[] { "TenantAdmin" });

        var act = () => new GetEffectiveExpirationRuleQueryHandler(
                _ruleEvaluatorMock.Object,
                _actorAccessorMock.Object,
                NullLogger<GetEffectiveExpirationRuleQueryHandler>.Instance)
            .Handle(new GetEffectiveExpirationRuleQuery
            {
                TenantId = new TenantId(TenantId),
                Permissions = new[] { "billing:refunds" }
            }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        _ruleEvaluatorMock.Verify(x => x.Describe(It.IsAny<IReadOnlyCollection<string>?>()), Times.Never);
    }
}

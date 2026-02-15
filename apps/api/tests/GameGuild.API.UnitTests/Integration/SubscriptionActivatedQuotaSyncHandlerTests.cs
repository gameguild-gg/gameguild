using FluentAssertions;
using GameGuild.API.Integration;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Resources;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameGuild.API.UnitTests.Integration;

public class SubscriptionActivatedQuotaSyncHandlerTests
{
    private readonly Mock<ISubscriptionRepository> _subRepoMock = new();
    private readonly Mock<IResourceQuotaService> _quotaServiceMock = new();
    private readonly Mock<ILogger<SubscriptionActivatedQuotaSyncHandler>> _loggerMock = new();
    private readonly SubscriptionActivatedQuotaSyncHandler _handler;

    public SubscriptionActivatedQuotaSyncHandlerTests()
    {
        _handler = new SubscriptionActivatedQuotaSyncHandler(
            _subRepoMock.Object,
            _quotaServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ShouldSkip()
    {
        var evt = new SubscriptionActivatedEvent(Guid.NewGuid(), Guid.NewGuid());
        _subRepoMock.Setup(r => r.GetByIdAsync(evt.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        await _handler.Handle(evt, CancellationToken.None);

        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(),
                It.IsAny<long?>(), It.IsAny<long?>(),
                It.IsAny<ResourceQuotaPeriod>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionHasNoPlan_ShouldSkip()
    {
        var evt = new SubscriptionActivatedEvent(Guid.NewGuid(), Guid.NewGuid());
        var subscription = CreateSubscriptionWithNullPlan();
        _subRepoMock.Setup(r => r.GetByIdAsync(evt.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await _handler.Handle(evt, CancellationToken.None);

        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(),
                It.IsAny<long?>(), It.IsAny<long?>(),
                It.IsAny<ResourceQuotaPeriod>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPlanHasMaxUsers_ShouldSetUsersQuota()
    {
        var evt = new SubscriptionActivatedEvent(Guid.NewGuid(), Guid.NewGuid());
        var plan = CreatePlan(maxUsers: 50);
        var subscription = CreateSubscriptionWithPlan(plan);
        _subRepoMock.Setup(r => r.GetByIdAsync(evt.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await _handler.Handle(evt, CancellationToken.None);

        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(evt.TenantId, ResourceUsageType.Users,
                40L, 50L, ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPlanHasMaxApiCalls_ShouldSetApiCallsQuota()
    {
        var evt = new SubscriptionActivatedEvent(Guid.NewGuid(), Guid.NewGuid());
        var plan = CreatePlan(maxApiCalls: 5000);
        var subscription = CreateSubscriptionWithPlan(plan);
        _subRepoMock.Setup(r => r.GetByIdAsync(evt.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        await _handler.Handle(evt, CancellationToken.None);

        var expectedSoftLimit = (long)(5000 * 0.8);
        _quotaServiceMock.Verify(
            s => s.SetQuotaAsync(evt.TenantId, ResourceUsageType.ApiCalls,
                expectedSoftLimit, 5000L, ResourceQuotaPeriod.Monthly, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenQuotaSetFails_ShouldNotThrow()
    {
        var evt = new SubscriptionActivatedEvent(Guid.NewGuid(), Guid.NewGuid());
        var plan = CreatePlan(maxUsers: 10);
        var subscription = CreateSubscriptionWithPlan(plan);
        _subRepoMock.Setup(r => r.GetByIdAsync(evt.SubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _quotaServiceMock.Setup(s => s.SetQuotaAsync(
                It.IsAny<Guid>(), It.IsAny<ResourceUsageType>(),
                It.IsAny<long?>(), It.IsAny<long?>(),
                It.IsAny<ResourceQuotaPeriod>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service down"));

        var act = () => _handler.Handle(evt, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // Helpers
    private static SubscriptionPlan CreatePlan(
        int? maxUsers = null, long? maxStorageMb = null, long? maxApiCalls = null)
    {
        var plan = new SubscriptionPlan("Test Plan", "test-plan", 1000);
        plan.MaxUsers = maxUsers;
        plan.MaxStorageMb = maxStorageMb;
        plan.MaxApiCallsPerMonth = maxApiCalls;
        return plan;
    }

    private static Subscription CreateSubscriptionWithNullPlan()
    {
        // Create subscription - Plan will be null by default since it's a navigation property
        var plan = CreatePlan();
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: plan.Id,
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(10m),
            startDate: DateTime.UtcNow);
        // Plan nav property is null when not eagerly loaded
        return subscription;
    }

    private static Subscription CreateSubscriptionWithPlan(SubscriptionPlan plan)
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: plan.Id,
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(10m),
            startDate: DateTime.UtcNow);
        // Use reflection to set the Plan navigation property
        var planProperty = typeof(Subscription).GetProperty("Plan");
        planProperty?.SetValue(subscription, plan);
        return subscription;
    }
}

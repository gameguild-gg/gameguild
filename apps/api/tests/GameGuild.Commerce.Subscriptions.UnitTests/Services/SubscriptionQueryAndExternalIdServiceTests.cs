using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Services;

public class SubscriptionQueryAndExternalIdServiceTests
{
    private readonly Mock<ISubscriptionRepository> _repository = new();
    private readonly Mock<ISubscriptionPlanService> _planService = new();
    private readonly Mock<ILogger<SubscriptionQueryAndExternalIdService>> _logger = new();
    private readonly SubscriptionQueryAndExternalIdService _service;

    public SubscriptionQueryAndExternalIdServiceTests()
    {
        _service = new SubscriptionQueryAndExternalIdService(_repository.Object, _planService.Object, _logger.Object);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenRepositoryIsNull()
    {
        var act = () => new SubscriptionQueryAndExternalIdService(null!, _planService.Object, _logger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenPlanServiceIsNull()
    {
        var act = () => new SubscriptionQueryAndExternalIdService(_repository.Object, null!, _logger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("planService");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        var act = () => new SubscriptionQueryAndExternalIdService(_repository.Object, _planService.Object, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task ExplicitGetByExternalIdAsync_ShouldDelegateToRepository()
    {
        var expected = CreateActiveSubscription();
        var contract = (ISubscriptionExternalIdService)_service;

        _repository.Setup(r => r.GetByExternalIdAsync("sub_external", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await contract.GetByExternalIdAsync("sub_external", CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldRequireAnActiveSubscription()
    {
        _repository.Setup(r => r.GetActiveTenantSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subscription?)null);

        var result = await _service.ValidateSubscriptionLimitsAsync(Guid.NewGuid(), 1, 100, 1000);

        result.IsWithinLimits.Should().BeFalse();
        result.RecommendedAction.Should().Be("Please subscribe to a plan");
        result.LimitChecks.Should().ContainSingle(check => check.LimitName == "Subscription" && check.Passed == false);
    }

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldReturnValid_WhenPlanIsUnlimited()
    {
        var subscription = CreateActiveSubscription();
        var plan = CreatePlan();

        _repository.Setup(r => r.GetActiveTenantSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planService.Setup(s => s.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var result = await _service.ValidateSubscriptionLimitsAsync(Guid.NewGuid(), 250, 5000, 250000);

        result.IsWithinLimits.Should().BeTrue();
        result.LimitChecks.Should().BeEmpty();
        result.Message.Should().Be("All limits are within allowed ranges");
    }

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldReturnValid_WhenUsageIsWithinConfiguredLimits()
    {
        var subscription = CreateActiveSubscription();
        var plan = CreatePlan();
        plan.UpdateLimits(maxUsers: 10, maxStorageMb: 1024, maxApiCallsPerMonth: 10000);

        _repository.Setup(r => r.GetActiveTenantSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planService.Setup(s => s.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var result = await _service.ValidateSubscriptionLimitsAsync(Guid.NewGuid(), 10, 1024, 10000);

        result.IsWithinLimits.Should().BeTrue();
        result.LimitChecks.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateSubscriptionLimitsAsync_ShouldReturnAllFailedChecks_WhenLimitsAreExceeded()
    {
        var subscription = CreateActiveSubscription();
        var plan = CreatePlan();
        plan.UpdateLimits(maxUsers: 10, maxStorageMb: 1024, maxApiCallsPerMonth: 10000);

        _repository.Setup(r => r.GetActiveTenantSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _planService.Setup(s => s.GetByIdAsync(subscription.PlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var result = await _service.ValidateSubscriptionLimitsAsync(Guid.NewGuid(), 11, 2048, 10001);

        result.IsWithinLimits.Should().BeFalse();
        result.RecommendedAction.Should().Be("Consider upgrading your plan");
        result.LimitChecks.Select(check => check.LimitName).Should().BeEquivalentTo(["Users", "Storage", "API Calls"]);
    }

    private static Subscription CreateActiveSubscription()
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow,
            trialEndDate: null);

        typeof(Subscription).GetProperty(nameof(Subscription.Id))!.SetValue(subscription, Guid.NewGuid());
        subscription.Activate();
        return subscription;
    }

    private static SubscriptionPlan CreatePlan()
    {
        var plan = new SubscriptionPlan("Growth", "growth", 2999, "USD", "Growth plan");
        typeof(SubscriptionPlan).GetProperty(nameof(SubscriptionPlan.Id))!.SetValue(plan, Guid.NewGuid());
        return plan;
    }
}

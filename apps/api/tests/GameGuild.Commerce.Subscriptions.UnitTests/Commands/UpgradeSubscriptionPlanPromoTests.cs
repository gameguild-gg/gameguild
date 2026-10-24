using FluentAssertions;
using GameGuild.Commerce;
using GameGuild.Commerce.Subscriptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Tests.Commerce.Subscriptions.Unit.Commands;

/// <summary>
/// Issue #418 promo application in plan upgrade/change flows: codes are quoted against
/// the target plan before the mutation (total rejection fails the command), the discount
/// is applied to the prorated charge after a successful upgrade, and usage rows are
/// recorded against the subscription.
/// </summary>
public class UpgradeSubscriptionPlanPromoTests
{
    private readonly Mock<ISubscriptionLifecycleService> _lifecycle = new();
    private readonly Mock<ISubscriptionPlanRepository> _plans = new();
    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<ISubscriptionPromoCoordinator> _coordinator = new();

    private static (Subscription Subscription, SubscriptionPlan OldPlan, SubscriptionPlan NewPlan) Arrange(
        decimal oldMonthly, decimal newMonthly, BillingCycle cycle = BillingCycle.Monthly)
    {
        var tenantId = Guid.NewGuid();
        var oldPlan = Plan(oldMonthly);
        var newPlan = Plan(newMonthly);
        var subscription = new Subscription(
            tenantId,
            oldPlan.Id,
            Guid.NewGuid(),
            cycle,
            new Money(oldMonthly),
            SystemClock.UtcNow.AddDays(-10),
            null);
        return (subscription, oldPlan, newPlan);
    }

    private static SubscriptionPlan Plan(decimal monthly) =>
        new($"Plan {monthly:N0}", $"plan-{monthly:N0}", (long)(monthly * 100), "USD")
        {
            Type = SubscriptionPlanType.Paid
        };

    private UpgradeSubscriptionPlanCommandHandler CreateHandler() => new(
        _lifecycle.Object,
        _plans.Object,
        _subscriptions.Object,
        NullLogger<UpgradeSubscriptionPlanCommandHandler>.Instance,
        _coordinator.Object);

    [Fact]
    public async Task Upgrade_WithCodes_QuotesBeforeMutation_AndFailsClosedOnTotalRejection()
    {
        var (subscription, _, newPlan) = Arrange(10m, 25m);
        _subscriptions.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _plans.Setup(r => r.GetByIdAsync(newPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPlan);
        _coordinator.Setup(c => c.QuoteForPlanChangeAsync(It.IsAny<SubscriptionPlanChangePromoQuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPromoQuoteResult(
                25m, 25m, 0,
                [],
                [new SubscriptionPromoRejectedCode("SAVE5", "Promo code has expired")]));

        var act = async () => await CreateHandler().Handle(
            new UpgradeSubscriptionPlanCommand(subscription.Id, newPlan.Id, PromoCodes: ["SAVE5"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*SAVE5*");
        _lifecycle.Verify(l => l.UpgradePlanAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never,
            "a fully rejected quote must fail before any mutation");
        _coordinator.Verify(c => c.RecordSubscriptionUsageAsync(It.IsAny<SubscriptionPromoUsageRecordRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upgrade_WithAppliedCodes_DiscountsProratedCharge_AndRecordsUsage()
    {
        var (subscription, oldPlan, newPlan) = Arrange(10m, 25m);
        _subscriptions.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _plans.Setup(r => r.GetByIdAsync(newPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPlan);
        _coordinator.Setup(c => c.QuoteForPlanChangeAsync(It.IsAny<SubscriptionPlanChangePromoQuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPromoQuoteResult(
                25m, 20m, 5m,
                [new SubscriptionPromoCodeApplication(Guid.NewGuid(), "SAVE5", 5m, "PercentageOff")],
                []));
        SubscriptionPromoUsageRecordRequest? recorded = null;
        _coordinator
            .Setup(c => c.RecordSubscriptionUsageAsync(It.IsAny<SubscriptionPromoUsageRecordRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionPromoUsageRecordRequest, CancellationToken>((request, _) => recorded = request)
            .ReturnsAsync([Guid.NewGuid()]);

        var tenantId = subscription.TenantId!.Value;
        var upgraded = new Subscription(
            tenantId, newPlan.Id, subscription.CreatedByUserId, BillingCycle.Monthly,
            new Money(25m), SystemClock.UtcNow, null);
        _lifecycle
            .Setup(l => l.UpgradePlanAsync(subscription.Id, newPlan.Id, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionUpgradeResult.CreateSuccess(upgraded, new Money(15m), new Money(5m)));

        var result = await CreateHandler().Handle(
            new UpgradeSubscriptionPlanCommand(subscription.Id, newPlan.Id, PromoCodes: ["SAVE5"]),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.PromoDiscountApplied.Should().Be(5m);
        result.DiscountedProratedAmount!.Amount.Should().Be(10m);
        result.AppliedPromoCodes.Should().ContainSingle(c => c.Code == "SAVE5");
        recorded.Should().NotBeNull();
        recorded!.SubscriptionId.Should().Be(subscription.Id);
        recorded.AppliedCodes.Should().ContainSingle(c => c.DiscountAmount == 5m);
    }

    [Fact]
    public async Task Upgrade_DiscountCappedAtCharge_WhenQuoteExceedsProration()
    {
        var (subscription, _, newPlan) = Arrange(10m, 25m);
        _subscriptions.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _plans.Setup(r => r.GetByIdAsync(newPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPlan);
        _coordinator.Setup(c => c.QuoteForPlanChangeAsync(It.IsAny<SubscriptionPlanChangePromoQuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPromoQuoteResult(
                25m, 5m, 20m,
                [new SubscriptionPromoCodeApplication(Guid.NewGuid(), "BIGSAVE", 20m, "FixedAmountOff")],
                []));
        _coordinator
            .Setup(c => c.RecordSubscriptionUsageAsync(It.IsAny<SubscriptionPromoUsageRecordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Guid.NewGuid()]);

        var tenantId = subscription.TenantId!.Value;
        var upgraded = new Subscription(
            tenantId, newPlan.Id, subscription.CreatedByUserId, BillingCycle.Monthly,
            new Money(25m), SystemClock.UtcNow, null);
        // Prorated upgrade charge of 8; the 20 quote must be capped to 8.
        _lifecycle
            .Setup(l => l.UpgradePlanAsync(subscription.Id, newPlan.Id, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionUpgradeResult.CreateSuccess(upgraded, new Money(8m), new Money(2m)));

        var result = await CreateHandler().Handle(
            new UpgradeSubscriptionPlanCommand(subscription.Id, newPlan.Id, PromoCodes: ["BIGSAVE"]),
            CancellationToken.None);

        result.PromoDiscountApplied.Should().Be(8m);
        result.DiscountedProratedAmount!.Amount.Should().Be(0m);
        result.AppliedPromoCodes.Should().ContainSingle().Which.DiscountAmount.Should().Be(8m);
    }

    [Fact]
    public async Task Upgrade_WithoutCodes_KeepsLegacyBehavior()
    {
        var (subscription, _, newPlan) = Arrange(10m, 25m);
        var tenantId = subscription.TenantId!.Value;
        var upgraded = new Subscription(
            tenantId, newPlan.Id, subscription.CreatedByUserId, BillingCycle.Monthly,
            new Money(25m), SystemClock.UtcNow, null);
        _lifecycle
            .Setup(l => l.UpgradePlanAsync(subscription.Id, newPlan.Id, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionUpgradeResult.CreateSuccess(upgraded, new Money(15m), new Money(5m)));

        var result = await CreateHandler().Handle(
            new UpgradeSubscriptionPlanCommand(subscription.Id, newPlan.Id),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AppliedPromoCodes.Should().BeEmpty();
        _coordinator.Verify(c => c.QuoteForPlanChangeAsync(It.IsAny<SubscriptionPlanChangePromoQuoteRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upgrade_WithCodes_ButNoCoordinator_Throws()
    {
        var (subscription, _, newPlan) = Arrange(10m, 25m);
        var handler = new UpgradeSubscriptionPlanCommandHandler(
            _lifecycle.Object, _plans.Object, _subscriptions.Object, null, promoCoordinator: null);

        var act = async () => await handler.Handle(
            new UpgradeSubscriptionPlanCommand(subscription.Id, newPlan.Id, PromoCodes: ["SAVE5"]),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no promo coordinator*");
    }

    [Fact]
    public async Task Downgrade_CreditOnly_ReturnsCodesRejected()
    {
        var (subscription, _, newPlan) = Arrange(25m, 10m);
        _subscriptions.Setup(r => r.GetByIdAsync(subscription.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);
        _plans.Setup(r => r.GetByIdAsync(newPlan.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newPlan);
        _coordinator.Setup(c => c.QuoteForPlanChangeAsync(It.IsAny<SubscriptionPlanChangePromoQuoteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscriptionPromoQuoteResult(
                10m, 8m, 2m,
                [new SubscriptionPromoCodeApplication(Guid.NewGuid(), "SAVE2", 2m, "FixedAmountOff")],
                []));

        var downgraded = new Subscription(
            tenantId, newPlan.Id, subscription.CreatedByUserId, BillingCycle.Monthly,
            new Money(10m), SystemClock.UtcNow, null);
        _lifecycle
            .Setup(l => l.DowngradePlanAsync(subscription.Id, newPlan.Id, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SubscriptionDowngradeResult.CreateSuccess(downgraded, SystemClock.UtcNow, new Money(6m)));

        var handler = new DowngradeSubscriptionPlanCommandHandler(
            _lifecycle.Object, _plans.Object, _subscriptions.Object,
            NullLogger<DowngradeSubscriptionPlanCommandHandler>.Instance, _coordinator.Object);

        var result = await handler.Handle(
            new DowngradeSubscriptionPlanCommand(subscription.Id, newPlan.Id, PromoCodes: ["SAVE2"]),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.PromoDiscountApplied.Should().Be(0m);
        result.RejectedPromoCodes.Should().ContainSingle()
            .Which.Reason.Should().Contain("no charge to discount");
        _coordinator.Verify(c => c.RecordSubscriptionUsageAsync(It.IsAny<SubscriptionPromoUsageRecordRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

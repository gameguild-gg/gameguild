using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Entities;

public sealed class SubscriptionAuthoritativeBillingTests
{
    private static readonly DateTime PaymentDate = new(2026, 7, 18, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(29.98)]
    [InlineData(30.00)]
    public void RecordPayment_ShouldReject_WhenAmountDoesNotExactlyMatchAuthoritativeAmount(decimal paidAmount)
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.RecordPayment(paidAmount, "USD", PaymentDate, "payment-1", forBillingCycle: 1);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("amount");
        subscription.BillingCycleCount.Should().Be(0);
        subscription.LastPaymentAt.Should().BeNull();
    }

    [Fact]
    public void RecordPayment_ShouldReject_WhenCurrencyDoesNotExactlyMatchAuthoritativeCurrency()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.RecordPayment(29.99m, "EUR", PaymentDate, "payment-1", forBillingCycle: 1);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("currency");
        subscription.BillingCycleCount.Should().Be(0);
        subscription.LastPaymentAt.Should().BeNull();
    }

    [Fact]
    public void RecordPayment_ShouldReject_WhenBillingCycleIdentityIsMissing()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.RecordPayment(29.99m, "USD", PaymentDate, "payment-1");

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("billing cycle");
        subscription.BillingCycleCount.Should().Be(0);
    }

    [Fact]
    public void RecordPayment_ShouldBeIdempotent_ForSamePaymentIdAndCycle()
    {
        var subscription = CreateActiveSubscription();
        subscription.RecordPayment(29.99m, "USD", PaymentDate, "payment-1", forBillingCycle: 1);

        var result = subscription.RecordPayment(29.99m, "USD", PaymentDate.AddMinutes(5), "payment-1", forBillingCycle: 1);

        result.IsAlreadyProcessed.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(1);
        subscription.LastPaymentAt.Should().Be(PaymentDate);
    }

    [Fact]
    public void RecordPayment_ShouldRejectDifferentPaymentId_ForAlreadyPaidCycle()
    {
        var subscription = CreateActiveSubscription();
        subscription.RecordPayment(29.99m, "USD", PaymentDate, "payment-1", forBillingCycle: 1);

        var result = subscription.RecordPayment(29.99m, "USD", PaymentDate.AddMinutes(5), "payment-2", forBillingCycle: 1);

        result.IsSuccess.Should().BeFalse();
        result.IsRejectedOutOfOrder.Should().BeTrue();
        result.Message.Should().Contain("different payment");
        subscription.BillingCycleCount.Should().Be(1);
        subscription.LastPaymentIdempotencyKey.Should().Be("payment-1");
        subscription.LastPaymentAt.Should().Be(PaymentDate);
    }

    [Fact]
    public void RecordPayment_ShouldRejectSkippedFutureCycle()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.RecordPayment(29.99m, "USD", PaymentDate, "payment-2", forBillingCycle: 2);

        result.IsSuccess.Should().BeFalse();
        result.IsRejectedOutOfOrder.Should().BeTrue();
        result.Message.Should().Contain("expected cycle 1");
        subscription.BillingCycleCount.Should().Be(0);
    }

    [Fact]
    public void RecordPayment_ShouldRejectStaleCycle()
    {
        var subscription = CreateActiveSubscription();
        subscription.RecordPayment(29.99m, "USD", PaymentDate, "payment-1", forBillingCycle: 1);

        var result = subscription.RecordPayment(29.99m, "USD", PaymentDate.AddMonths(1), "stale-payment", forBillingCycle: 0);

        result.IsSuccess.Should().BeFalse();
        result.IsRejectedOutOfOrder.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(1);
    }

    [Fact]
    public void RecordPayment_ShouldRejectCancelledSubscription()
    {
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested);

        var result = subscription.RecordPayment(29.99m, "USD", PaymentDate, "payment-1", forBillingCycle: 1);

        result.IsRejectedCancelled.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(0);
    }

    [Fact]
    public void RecordPayment_ShouldRejectExpiredSubscription()
    {
        var subscription = CreateActiveSubscription();
        typeof(Subscription).GetProperty(nameof(Subscription.Status))!
            .SetValue(subscription, SubscriptionStatus.Expired);

        var result = subscription.RecordPayment(29.99m, "USD", PaymentDate, "payment-1", forBillingCycle: 1);

        result.IsRejectedCancelled.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(0);
    }

    [Fact]
    public void ProcessRenewal_ShouldReturnPaymentRequiredWithoutAdvancingOrClaimingRevenue()
    {
        var subscription = CreateActiveSubscription();
        var initialPeriodStart = subscription.CurrentPeriodStart;
        var initialPeriodEnd = subscription.CurrentPeriodEnd;
        var initialNextBillingDate = subscription.NextBillingDate;

        var result = subscription.ProcessRenewal(new Money(29.99m, "USD"), "renewal-1");

        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("payment confirmation");
        result.ChargedAmount.Should().BeNull();
        subscription.BillingCycleCount.Should().Be(0);
        subscription.CurrentPeriodStart.Should().Be(initialPeriodStart);
        subscription.CurrentPeriodEnd.Should().Be(initialPeriodEnd);
        subscription.NextBillingDate.Should().Be(initialNextBillingDate);
        subscription.LastRenewalIdempotencyKey.Should().BeNull();
    }

    [Fact]
    public void RecordPayment_ShouldAdvanceRenewalPeriod_OnlyAfterExactCyclePaymentConfirmation()
    {
        var subscription = CreateActiveSubscription();
        subscription.RecordPayment(29.99m, "USD", PaymentDate, "payment-1", forBillingCycle: 1);
        var periodEndBeforeRenewal = subscription.CurrentPeriodEnd;
        var nextBillingBeforeRenewal = subscription.NextBillingDate;

        var result = subscription.RecordPayment(29.99m, "USD", PaymentDate.AddMonths(1), "payment-2", forBillingCycle: 2);

        result.IsSuccess.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(2);
        subscription.CurrentPeriodStart.Should().Be(nextBillingBeforeRenewal);
        subscription.CurrentPeriodEnd.Should().BeAfter(periodEndBeforeRenewal);
        subscription.NextBillingDate.Should().BeAfter(nextBillingBeforeRenewal);
    }

    private static Subscription CreateActiveSubscription()
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: PaymentDate.AddMonths(-1));
        subscription.Activate();

        return subscription;
    }
}

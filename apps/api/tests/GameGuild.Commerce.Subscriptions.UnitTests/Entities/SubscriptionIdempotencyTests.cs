using FluentAssertions;

using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Entities;

/// <summary>
///     Tests for Subscription entity idempotency and out-of-order protection.
///     These tests verify the critical financial invariants documented in COMMERCE_MODULES_SECURITY_AUDIT.md:
///     - Invariant #5: Subscriptions never generate duplicate charges
///     - Attack Scenario #1: Webhook Retry Duplicate Charge
///     - Attack Scenario #5: Out-of-Order Payments
/// </summary>
public class SubscriptionIdempotencyTests
{
    #region RecordPayment Idempotency Tests

    [Fact]
    public void RecordPayment_WithSameIdempotencyKey_ShouldReturnAlreadyProcessed()
    {
        // Arrange - Invariant #5: Subscriptions never generate duplicate charges
        var subscription = CreateActiveSubscription();
        var idempotencyKey = "payment_key_12345";
        var paymentDate = DateTime.UtcNow;

        // First payment
        var firstResult = subscription.RecordPayment(29.99m, "USD", paymentDate, idempotencyKey, forBillingCycle: 1);

        // Act - Second payment with same key (simulates webhook retry)
        var secondResult = subscription.RecordPayment(29.99m, "USD", paymentDate, idempotencyKey, forBillingCycle: 1);

        // Assert
        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeFalse();
        secondResult.IsAlreadyProcessed.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(1, "duplicate payment should not increment billing cycle");
    }

    [Fact]
    public void RecordPayment_WithDifferentIdempotencyKey_ShouldProcess()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var paymentDate = DateTime.UtcNow;

        // Act
        var firstResult = subscription.RecordPayment(29.99m, "USD", paymentDate, "key_1", forBillingCycle: 1);
        var secondResult = subscription.RecordPayment(29.99m, "USD", paymentDate.AddMonths(1), "key_2", forBillingCycle: 2);

        // Assert
        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(2);
    }

    [Fact]
    public void RecordPayment_WithNullIdempotencyKey_ShouldThrow()
    {
        // Arrange - Idempotency key is required for all payments
        var subscription = CreateActiveSubscription();

        // Act & Assert
        var act = () => subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, null!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*idempotency*");
    }

    [Fact]
    public void RecordPayment_WithEmptyIdempotencyKey_ShouldThrow()
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act & Assert
        var act = () => subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*idempotency*");
    }

    #endregion

    #region Out-of-Order Payment Protection Tests

    [Fact]
    public void RecordPayment_ForPastBillingCycle_ShouldRejectAsOutOfOrder()
    {
        // Arrange - Attack Scenario #5: Out-of-Order Payments
        var subscription = CreateActiveSubscription();

        // Process cycle 1 and 2
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "key_cycle_1", forBillingCycle: 1);
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow.AddMonths(1), "key_cycle_2", forBillingCycle: 2);

        // Act - Try to process cycle 1 again (out-of-order)
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "key_cycle_1_late", forBillingCycle: 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsRejectedOutOfOrder.Should().BeTrue();
        result.LastProcessedBillingCycle.Should().Be(2);
    }

    [Fact]
    public void RecordPayment_ForCurrentBillingCycle_WithDifferentKey_ShouldRejectDifferentPayment()
    {
        // Arrange - Edge case: same cycle, different key (could be retry with new payment attempt)
        var subscription = CreateActiveSubscription();

        // Process cycle 1
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "key_attempt_1", forBillingCycle: 1);

        // Act - Same cycle, different key
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "key_attempt_2", forBillingCycle: 1);

        result.IsRejectedOutOfOrder.Should().BeTrue();
        subscription.LastPaymentIdempotencyKey.Should().Be("key_attempt_1");
    }

    [Fact]
    public void RecordPayment_WithoutBillingCycle_ShouldReject()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "key_1");

        result.IsSuccess.Should().BeFalse();
        result.IsRejectedOutOfOrder.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(0);
        subscription.LastProcessedBillingCycle.Should().Be(0);
    }

    #endregion

    #region ProcessRenewal Idempotency Tests

    [Fact]
    public void ProcessRenewal_WithSameIdempotencyKey_ShouldReturnExistingResult()
    {
        // Arrange - Attack Scenario #1: Webhook Retry Duplicate Charge
        var subscription = CreateActiveSubscription();
        var renewalAmount = new Money(29.99m, "USD");
        var idempotencyKey = "renewal_key_12345";

        // First renewal (BillingCycleCount goes from 0 to 1)
        var firstResult = subscription.ProcessRenewal(renewalAmount, idempotencyKey);
        var cycleAfterFirst = subscription.BillingCycleCount;

        // Act - Second renewal with same key (simulates webhook retry)
        var secondResult = subscription.ProcessRenewal(renewalAmount, idempotencyKey);

        firstResult.Success.Should().BeFalse();
        secondResult.Success.Should().BeFalse();
        firstResult.FailureReason.Should().Contain("payment confirmation");
        secondResult.SubscriptionId.Should().Be(firstResult.SubscriptionId);
        subscription.BillingCycleCount.Should().Be(cycleAfterFirst, "duplicate renewal should not increment cycle twice");
    }

    [Fact]
    public void ProcessRenewal_WithDifferentIdempotencyKey_ShouldNotAdvanceWithoutPayment()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var renewalAmount = new Money(29.99m, "USD");
        var initialCycle = subscription.BillingCycleCount;

        var firstResult = subscription.ProcessRenewal(renewalAmount, "key_1");
        var secondResult = subscription.ProcessRenewal(renewalAmount, "key_2");

        firstResult.Success.Should().BeFalse();
        secondResult.Success.Should().BeFalse();
        subscription.BillingCycleCount.Should().Be(initialCycle);
    }

    [Fact]
    public void ProcessRenewal_ShouldNotClaimRenewalIdempotencyKeyWithoutPayment()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var renewalAmount = new Money(29.99m, "USD");
        var idempotencyKey = "renewal_unique_key";

        // Act
        subscription.ProcessRenewal(renewalAmount, idempotencyKey);

        // Assert
        subscription.LastRenewalIdempotencyKey.Should().BeNull();
    }

    #endregion

    #region Price Version Locking Tests

    [Fact]
    public void LockToPriceVersion_ShouldSetLockedPriceVersionId()
    {
        // Arrange - Attack Scenario #4: Price Change Affecting Subscriptions
        var subscription = CreateActiveSubscription();
        var priceVersionId = Guid.NewGuid();

        // Act
        subscription.LockToPriceVersion(priceVersionId);

        // Assert
        subscription.LockedPriceVersionId.Should().Be(priceVersionId);
    }

    [Fact]
    public void UnlockPriceVersion_ShouldClearLockedPriceVersionId()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var priceVersionId = Guid.NewGuid();
        subscription.LockToPriceVersion(priceVersionId);

        // Act
        subscription.UnlockPriceVersion();

        // Assert
        subscription.LockedPriceVersionId.Should().BeNull();
    }

    [Fact]
    public void UnlockPriceVersion_WhenNotLocked_ShouldBeNoOp()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.LockedPriceVersionId.Should().BeNull("initially not locked");

        // Act - Unlocking when not locked is a no-op (safe idempotent behavior)
        subscription.UnlockPriceVersion();

        // Assert - Should still be null, no exception thrown
        subscription.LockedPriceVersionId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithLockedPriceVersionId_ShouldSetProperty()
    {
        // Arrange
        var priceVersionId = Guid.NewGuid();

        // Act
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow,
            trialEndDate: null,
            lockedPriceVersionId: priceVersionId
        );

        // Assert
        subscription.LockedPriceVersionId.Should().Be(priceVersionId);
    }

    #endregion

    #region Helper Methods

    private static Subscription CreateActiveSubscription()
    {
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow
        );
        subscription.Activate();
        return subscription;
    }

    #endregion
}

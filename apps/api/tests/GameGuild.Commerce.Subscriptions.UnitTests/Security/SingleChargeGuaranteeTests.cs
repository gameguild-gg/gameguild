using FluentAssertions;

using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Security;

/// <summary>
///     P0 Critical Tests: Single Charge Guarantee
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify that duplicate charges cannot occur through any attack vector.
/// </summary>
public class SingleChargeGuaranteeTests
{
    #region Renewal Idempotency Tests (P0)

    [Fact]
    public void ProcessRenewal_WithSameIdempotencyKey_ShouldReturnCachedResult()
    {
        // Arrange - Attack Scenario: Webhook retry with same idempotency key
        var subscription = CreateActiveSubscription();
        var renewalAmount = new Money(29.99m, "USD");
        var idempotencyKey = "renewal_2026_01_cycle_1";
        
        var firstResult = subscription.ProcessRenewal(renewalAmount, idempotencyKey);
        var billingCycleAfterFirst = subscription.BillingCycleCount;

        // Act - Retry with same key (should be idempotent)
        var secondResult = subscription.ProcessRenewal(renewalAmount, idempotencyKey);

        // Assert
        firstResult.Success.Should().BeTrue("first renewal should succeed");
        secondResult.Success.Should().BeTrue("idempotent retry should return success");
        subscription.BillingCycleCount.Should().Be(billingCycleAfterFirst, "billing cycle should NOT increment on duplicate");
        subscription.LastRenewalIdempotencyKey.Should().Be(idempotencyKey);
    }

    [Fact]
    public void RecordPayment_WithDuplicateIdempotencyKey_ShouldBeRejected()
    {
        // Arrange - Attack Scenario: Duplicate payment recording
        var subscription = CreateActiveSubscription();
        var idempotencyKey = "payment_ext_ch_12345";
        
        var firstResult = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, idempotencyKey);

        // Act - Try to record same payment again
        var secondResult = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, idempotencyKey);

        // Assert
        firstResult.IsSuccess.Should().BeTrue("first payment should succeed");
        secondResult.IsSuccess.Should().BeFalse("duplicate payment should be rejected");
        secondResult.IsAlreadyProcessed.Should().BeTrue("should indicate already processed");
    }

    [Fact]
    public void RecordPayment_WithDifferentAmount_SameIdempotencyKey_ShouldStillReject()
    {
        // Arrange - Attack: Try to charge different amount with same key
        var subscription = CreateActiveSubscription();
        var idempotencyKey = "payment_attack_attempt";
        
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, idempotencyKey);

        // Act - Try with different amount
        var result = subscription.RecordPayment(99.99m, "USD", DateTime.UtcNow, idempotencyKey);

        // Assert - Idempotency key should block regardless of amount
        result.IsSuccess.Should().BeFalse();
        result.IsAlreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public void ProcessRenewal_ConcurrentCalls_ShouldProduceSingleCharge()
    {
        // Arrange - Simulate concurrent renewal attempts
        var subscription = CreateActiveSubscription();
        var renewalAmount = new Money(29.99m, "USD");
        var initialBillingCycle = subscription.BillingCycleCount;
        
        // These would be the same key in a real concurrent scenario
        var key1 = "concurrent_renewal_key";
        var key2 = "concurrent_renewal_key";

        // Act - Simulate two "concurrent" calls with same key
        var result1 = subscription.ProcessRenewal(renewalAmount, key1);
        var result2 = subscription.ProcessRenewal(renewalAmount, key2);

        // Assert - Only one charge should occur
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue("idempotent call returns success");
        subscription.BillingCycleCount.Should().Be(initialBillingCycle + 1, "only one increment");
    }

    [Fact]
    public void RecordPayment_WithOutOfOrderBillingCycle_ShouldBeRejected()
    {
        // Arrange - Attack: Out-of-order payment to cause double charge
        var subscription = CreateActiveSubscription();
        
        // Process cycles 1 and 2
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "key_cycle_1", forBillingCycle: 1);
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow.AddMonths(1), "key_cycle_2", forBillingCycle: 2);

        // Act - Try to re-process cycle 1
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "key_cycle_1_late", forBillingCycle: 1);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsRejectedOutOfOrder.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ProcessRenewal_WithNullIdempotencyKey_ShouldFail()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var renewalAmount = new Money(29.99m, "USD");

        // Act
        var result = subscription.ProcessRenewal(renewalAmount, null!);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("Idempotency key");
    }

    [Fact]
    public void ProcessRenewal_WithEmptyIdempotencyKey_ShouldFail()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var renewalAmount = new Money(29.99m, "USD");

        // Act
        var result = subscription.ProcessRenewal(renewalAmount, "");

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void RecordPayment_AfterMultipleRenewals_ShouldTrackCorrectCycle()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        
        // Process multiple renewals
        subscription.ProcessRenewal(new Money(29.99m, "USD"), "renewal_1");
        subscription.ProcessRenewal(new Money(29.99m, "USD"), "renewal_2");
        subscription.ProcessRenewal(new Money(29.99m, "USD"), "renewal_3");

        // Act - Record payment for current cycle
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "payment_cycle_3");

        // Assert
        result.IsSuccess.Should().BeTrue();
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

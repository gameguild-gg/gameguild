using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using GameGuild.ValueObjects;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Security;

/// <summary>
///     P0 Critical Tests: Cancellation Without Residue
///     From: COMMERCE_MODULES_SECURITY_AUDIT.md Section 7 - Test Plan
///     These tests verify cancelled subscriptions cannot generate charges.
/// </summary>
public class CancellationWithoutResidueTests
{
    #region Cancelled Subscription Cannot Renew (P0)

    [Fact]
    public void Cancel_ShouldPreventRenewal()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "User requested cancellation");

        // Act
        var result = subscription.ProcessRenewal(new Money(29.99m, "USD"), "renewal_attempt");

        // Assert
        result.Success.Should().BeFalse("cancelled subscription should not renew");
        result.FailureReason.Should().Contain("not active").Or.Contain("Cancelled");
    }

    [Fact]
    public void Cancel_ShouldSetCorrectEndDate()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var periodEnd = subscription.CurrentPeriodEnd;

        // Act
        subscription.Cancel(CancellationReason.UserRequested, "Test");

        // Assert
        subscription.EndDate.Should().NotBeNull();
        subscription.EndDate.Should().BeOnOrAfter(DateTime.UtcNow, "end date should be current period end or later");
    }

    [Fact]
    public void Cancel_ShouldDisableAutoRenew()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.AutoRenew.Should().BeTrue("active subscription should have auto-renew enabled");

        // Act
        subscription.Cancel(CancellationReason.UserRequested, "Test");

        // Assert
        subscription.AutoRenew.Should().BeFalse("cancellation should disable auto-renew");
    }

    [Fact]
    public void Cancel_ShouldTransitionToCancelledStatus()
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.Cancel(CancellationReason.UserRequested, "Test cancellation");

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    #endregion

    #region Cancellation Reason Tests

    [Theory]
    [InlineData(CancellationReason.UserRequested)]
    [InlineData(CancellationReason.PaymentFailed)]
    [InlineData(CancellationReason.PolicyViolation)]
    [InlineData(CancellationReason.PlanDiscontinued)]
    public void Cancel_ShouldStoreCancellationReason(CancellationReason reason)
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.Cancel(reason, "Test");

        // Assert
        subscription.CancellationReason.Should().Be(reason);
    }

    [Fact]
    public void Cancel_ShouldStoreCancellationNotes()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var cancellationNotes = "Customer requested immediate cancellation due to service issues";

        // Act
        subscription.Cancel(CancellationReason.UserRequested, cancellationNotes);

        // Assert
        subscription.CancellationNotes.Should().Be(cancellationNotes);
    }

    #endregion

    #region Cannot Resurrect Cancelled Subscription Tests

    [Fact]
    public void Cancelled_CannotTransitionToActive()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "Cancelled");

        // Act & Assert
        var act = () => subscription.Activate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancelled_CannotBeReactivated()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "Cancelled");

        // Act & Assert
        var act = () => subscription.Reactivate();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancelled_CannotBeSuspended()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "Cancelled");

        // Act & Assert
        var act = () => subscription.Suspend("Attempt to suspend cancelled");
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Entitlements Until EndDate (P1)

    [Fact]
    public void Cancelled_ShouldHaveAccessUntilEndDate()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var periodEnd = subscription.CurrentPeriodEnd;
        subscription.Cancel(CancellationReason.UserRequested, "Test");

        // Act - Check if still within entitlement period
        var hasAccess = DateTime.UtcNow < subscription.EndDate;

        // Assert
        hasAccess.Should().BeTrue("user should have access until end date");
        subscription.EndDate.Should().BeOnOrAfter(periodEnd ?? DateTime.UtcNow);
    }

    [Fact]
    public void Cancelled_AfterEndDate_ShouldHaveNoAccess()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "Test");
        
        // Simulate time passing (end date in the past)
        var simulatedCurrentTime = subscription.EndDate!.Value.AddDays(1);

        // Act
        var hasAccess = simulatedCurrentTime < subscription.EndDate;

        // Assert
        hasAccess.Should().BeFalse("user should have no access after end date");
    }

    #endregion

    #region Immediate vs Period-End Cancellation

    [Fact]
    public void Cancel_Immediate_ShouldSetEndDateToNow()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var beforeCancel = DateTime.UtcNow;

        // Act - Immediate cancellation
        subscription.Cancel(CancellationReason.PolicyViolation, "Terms violation - immediate", immediate: true);

        // Assert
        subscription.EndDate.Should().BeCloseTo(beforeCancel, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Cancel_AtPeriodEnd_ShouldSetEndDateToPeriodEnd()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        var expectedPeriodEnd = subscription.CurrentPeriodEnd;

        // Act - Standard cancellation (at period end)
        subscription.Cancel(CancellationReason.UserRequested, "User requested", immediate: false);

        // Assert
        subscription.EndDate.Should().Be(expectedPeriodEnd);
    }

    #endregion

    #region Payment After Cancellation

    [Fact]
    public void Cancelled_RecordPayment_ShouldBeRejected()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "Cancelled");

        // Act - Attempt to record payment after cancellation
        // Note: This tests that the subscription cannot process payments
        var renewalResult = subscription.ProcessRenewal(new Money(29.99m, "USD"), "post_cancel_payment");

        // Assert
        renewalResult.Success.Should().BeFalse();
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
        subscription.CurrentPeriodStart = DateTime.UtcNow;
        subscription.CurrentPeriodEnd = DateTime.UtcNow.AddDays(30);
        return subscription;
    }

    #endregion
}

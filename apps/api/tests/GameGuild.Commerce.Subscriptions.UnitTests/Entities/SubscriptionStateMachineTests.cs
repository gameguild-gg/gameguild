using FluentAssertions;


using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Entities;

/// <summary>
///     Tests for Subscription state machine enforcement.
///     These tests verify Invariant #4: Financial state transitions are monotonic
/// </summary>
public class SubscriptionStateMachineTests
{
    #region Valid State Transitions

    [Fact]
    public void PendingActivation_CanTransitionTo_Active()
    {
        // Arrange
        var subscription = CreatePendingSubscription();

        // Act
        subscription.Activate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void PendingActivation_CanTransitionTo_Trialing()
    {
        // Arrange
        var subscription = CreatePendingSubscription();
        var trialEndDate = DateTime.UtcNow.AddDays(14);

        // Act
        subscription.StartTrial(trialEndDate);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
    }

    [Fact]
    public void Trialing_CanTransitionTo_Active()
    {
        // Arrange
        var subscription = CreateTrialingSubscription();

        // Act
        subscription.EndTrial(convertToPaid: true);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Trialing_CanTransitionTo_Cancelled()
    {
        // Arrange
        var subscription = CreateTrialingSubscription();

        // Act
        subscription.Cancel(CancellationReason.UserRequested, "User cancelled during trial");

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void Active_CanTransitionTo_Cancelled()
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.Cancel(CancellationReason.UserRequested, "User requested cancellation");

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void Active_CanTransitionTo_Suspended()
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.Suspend("Payment issue");

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
    }

    [Fact]
    public void Active_CanTransitionTo_PastDue()
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.RecordPaymentFailure("Card declined", DateTime.UtcNow);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public void Suspended_CanTransitionTo_Active()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Suspend("Test");

        // Act
        subscription.Reactivate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Suspended_CanTransitionTo_Cancelled()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Suspend("Test");

        // Act
        subscription.Cancel(CancellationReason.PaymentFailed, "Extended non-payment");

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void PastDue_CanTransitionTo_Active_AfterPayment()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.RecordPaymentFailure("Card declined", DateTime.UtcNow);
        subscription.Status.Should().Be(SubscriptionStatus.PastDue);

        // Act - Record payment, then reactivate (two-step process)
        var paymentResult = subscription.RecordPayment(
            29.99m,
            "USD",
            DateTime.UtcNow,
            "recovery_payment_key",
            forBillingCycle: 1);
        paymentResult.IsSuccess.Should().BeTrue("payment should be recorded successfully");
        subscription.Reactivate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    #endregion

    #region Invalid State Transitions

    [Fact]
    public void Cancelled_CannotTransitionTo_Active()
    {
        // Arrange - Invariant #4: Financial state transitions are monotonic
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "Cancelled");

        // Act & Assert
        var act = () => subscription.Activate();
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("*Cancelled*Active*");
    }

    [Fact]
    public void Cancelled_CannotBeReactivated()
    {
        // Arrange
        var subscription = CreateActiveSubscription();
        subscription.Cancel(CancellationReason.UserRequested, "Cancelled");

        // Act & Assert
        var act = () => subscription.Reactivate();
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("*Cancelled*Active*");
    }

    [Fact]
    public void PendingActivation_CannotBeSuspended()
    {
        // Arrange
        var subscription = CreatePendingSubscription();

        // Act & Assert
        var act = () => subscription.Suspend("Cannot suspend pending");
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("*PendingActivation*Suspended*");
    }

    [Fact]
    public void Trialing_CannotBeSuspended()
    {
        // Arrange
        var subscription = CreateTrialingSubscription();

        // Act & Assert
        var act = () => subscription.Suspend("Cannot suspend trial");
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("*Trialing*Suspended*");
    }

    [Fact]
    public void Active_CannotStartTrial()
    {
        // Arrange - Cannot go back to trial after activation
        var subscription = CreateActiveSubscription();

        // Act & Assert
        var act = () => subscription.StartTrial(DateTime.UtcNow.AddDays(14));
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("*Active*Trialing*");
    }

    #endregion

    #region CanTransitionTo Tests

    [Theory]
    [InlineData(SubscriptionStatus.PendingActivation, SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.PendingActivation, SubscriptionStatus.Trialing, true)]
    [InlineData(SubscriptionStatus.PendingActivation, SubscriptionStatus.Cancelled, true)]
    [InlineData(SubscriptionStatus.PendingActivation, SubscriptionStatus.Suspended, false)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Cancelled, true)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Suspended, true)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.PastDue, true)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.PendingActivation, false)]
    [InlineData(SubscriptionStatus.Active, SubscriptionStatus.Trialing, false)]
    [InlineData(SubscriptionStatus.Cancelled, SubscriptionStatus.Active, false)]
    [InlineData(SubscriptionStatus.Cancelled, SubscriptionStatus.Suspended, false)]
    public void CanTransitionTo_ShouldValidateTransitions(
        SubscriptionStatus from,
        SubscriptionStatus to,
        bool expectedResult)
    {
        // Arrange
        var subscription = CreateSubscriptionInStatus(from);

        // Act
        var canTransition = subscription.CanTransitionTo(to);

        // Assert
        canTransition.Should().Be(expectedResult);
    }

    #endregion

    #region Cancellation Reason Tests

    [Theory]
    [InlineData(CancellationReason.UserRequested)]
    [InlineData(CancellationReason.PaymentFailed)]
    [InlineData(CancellationReason.PolicyViolation)]
    [InlineData(CancellationReason.PlanDiscontinued)]
    [InlineData(CancellationReason.ExternalRequest)]
    public void Cancel_ShouldStoreCancellationReason(CancellationReason reason)
    {
        // Arrange
        var subscription = CreateActiveSubscription();

        // Act
        subscription.Cancel(reason, $"Cancelled due to: {reason}");

        // Assert
        subscription.CancellationReason.Should().Be(reason);
        subscription.CancellationNote.Should().Contain(reason.ToString());
        subscription.CancelledAt.Should().NotBeNull();
    }

    #endregion

    #region Helper Methods

    private static Subscription CreatePendingSubscription()
    {
        return new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow
        );
    }

    private static Subscription CreateTrialingSubscription()
    {
        var subscription = CreatePendingSubscription();
        subscription.StartTrial(DateTime.UtcNow.AddDays(14));
        return subscription;
    }

    private static Subscription CreateActiveSubscription()
    {
        var subscription = CreatePendingSubscription();
        subscription.Activate();
        return subscription;
    }

    private static Subscription CreateSubscriptionInStatus(SubscriptionStatus status)
    {
        var subscription = CreatePendingSubscription();

        switch (status)
        {
            case SubscriptionStatus.PendingActivation:
                return subscription;
            case SubscriptionStatus.Trialing:
                subscription.StartTrial(DateTime.UtcNow.AddDays(14));
                return subscription;
            case SubscriptionStatus.Active:
                subscription.Activate();
                return subscription;
            case SubscriptionStatus.Suspended:
                subscription.Activate();
                subscription.Suspend("Test");
                return subscription;
            case SubscriptionStatus.PastDue:
                subscription.Activate();
                subscription.RecordPaymentFailure("Test", DateTime.UtcNow);
                return subscription;
            case SubscriptionStatus.Cancelled:
                subscription.Activate();
                subscription.Cancel(CancellationReason.UserRequested, "Test");
                return subscription;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    #endregion
}

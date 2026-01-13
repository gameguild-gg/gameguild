using GameGuild.ValueObjects;
using FluentAssertions;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Commerce.Subscriptions;
using Xunit;

namespace GameGuild.Subscriptions.UnitTests.Entities;

public class SubscriptionTests
{
    [Fact]
    public void Constructor_ShouldCreateSubscription_WithValidParameters()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = new Money(2999);
        var startDate = DateTime.UtcNow;

        // Act
        var subscription = new Subscription(tenantId, planId, userId, BillingCycle.Monthly, amount, startDate);

        // Assert
        subscription.TenantId!.Value.Should().Be(tenantId);
        subscription.PlanId.Should().Be(planId);
        subscription.CreatedByUserId.Should().Be(userId);
        subscription.Amount.Should().Be(amount);
        subscription.StartDate.Should().Be(startDate);
        subscription.Status.Should().Be(SubscriptionStatus.PendingActivation);
    }

    [Fact]
    public void Constructor_WithTrialPeriod_ShouldSetTrialingStatus()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = new Money(2999);
        var startDate = DateTime.UtcNow;
        var trialEndDate = startDate.AddDays(14);

        // Act
        var subscription = new Subscription(tenantId, planId, userId, BillingCycle.Monthly, amount, startDate, trialEndDate);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.TrialEndDate.Should().Be(trialEndDate);
    }

    [Fact]
    public void Constructor_ShouldRaiseDomainEvent()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = new Money(2999);
        var startDate = DateTime.UtcNow;

        // Act
        var subscription = new Subscription(tenantId, planId, userId, BillingCycle.Monthly, amount, startDate);

        // Assert
        var events = subscription.DomainEvents;
        events.Should().ContainSingle();
        events.First().Should().BeOfType<SubscriptionCreatedEvent>();
    }

    [Fact]
    public void Activate_ShouldChangeStatus_ToActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();

        // Act
        subscription.Activate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Activate_ShouldRaiseDomainEvent()
    {
        // Arrange
        var subscription = CreateValidSubscription();

        // Act
        subscription.Activate();

        // Assert
        var events = subscription.DomainEvents;
        events.Should().Contain(e => e.GetType() == typeof(SubscriptionActivatedEvent));
    }

    [Fact]
    public void Cancel_ShouldChangeStatus_ToCancelled()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        subscription.Cancel(CancellationReason.UserRequested, "Test cancellation");

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancellationReason.Should().Be(CancellationReason.UserRequested);
        subscription.CancellationNote.Should().Be("Test cancellation");
        subscription.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldRaiseDomainEvent()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        subscription.Cancel(CancellationReason.UserRequested, "Test cancellation");

        // Assert
        var events = subscription.DomainEvents;
        events.Should().Contain(e => e.GetType() == typeof(SubscriptionCancelledEvent));
    }

    [Fact]
    public void Suspend_ShouldChangeStatus_ToSuspended()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        subscription.Suspend("Payment issue");

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_ShouldChangeStatus_ToActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Suspend("Test");

        // Act
        subscription.Reactivate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.AutoRenew.Should().BeTrue();
    }

    [Fact]
    public void RecordPayment_ShouldUpdateBillingInformation()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var paymentDate = DateTime.UtcNow;
        var amount = 29.99m;

        // Act
        subscription.RecordPayment(amount, "USD", paymentDate);

        // Assert
        subscription.LastPaymentAt.Should().Be(paymentDate);
        subscription.BillingCycleCount.Should().Be(1);
    }

    [Fact]
    public void RecordPaymentFailure_ShouldChangeStatus_ToPastDue()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var failureDate = DateTime.UtcNow;

        // Act
        subscription.RecordPaymentFailure("Card declined", failureDate);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
    }

    [Fact]
    public void SetAutoRenew_ShouldUpdateAutoRenewFlag()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        subscription.SetAutoRenew(false);

        // Assert
        subscription.AutoRenew.Should().BeFalse();
    }

    [Fact]
    public void StartTrial_ShouldSetTrialingStatus()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var trialEndDate = DateTime.UtcNow.AddDays(14);

        // Act
        subscription.StartTrial(trialEndDate);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.TrialEndDate.Should().Be(trialEndDate);
    }

    [Fact]
    public void EndTrial_WithConversion_ShouldActivateSubscription()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var trialEndDate = DateTime.UtcNow.AddDays(14);
        subscription.StartTrial(trialEndDate);

        // Act
        subscription.EndTrial(convertToPaid: true);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void SetExternalIds_ShouldUpdateExternalIds()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var subscriptionId = "sub_123456";
        var customerId = "cus_123456";

        // Act
        subscription.SetExternalIds(subscriptionId, customerId);

        // Assert
        subscription.ExternalId.Should().Be(subscriptionId);
        subscription.ExternalCustomerId.Should().Be(customerId);
    }

    [Fact]
    public void UpdateMetadata_ShouldStoreJsonMetadata()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var metadata = "{\"key\":\"value\"}";

        // Act
        subscription.UpdateMetadata(metadata);

        // Assert
        subscription.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void ChangePlan_ShouldUpdatePlanAndAmount()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(4999);

        // Act
        subscription.ChangePlan(newPlanId, newAmount);

        // Assert
        subscription.PlanId.Should().Be(newPlanId);
        subscription.Amount.Should().Be(newAmount);
    }

    private static Subscription CreateValidSubscription()
    {
        return new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow,
            trialEndDate: null
        );
    }
}

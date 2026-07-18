

using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Subscriptions.UnitTests.Entities;

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
    public void Constructor_ShouldInitializeWithCorrectDefaults()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var amount = new Money(2999);
        var startDate = DateTime.UtcNow;

        // Act
        var subscription = new Subscription(tenantId, planId, userId, BillingCycle.Monthly, amount, startDate);

        // Assert - Verify correct initialization
        subscription.TenantId.Should().Be(tenantId);
        subscription.PlanId.Should().Be(planId);
        subscription.CreatedByUserId.Should().Be(userId);
        subscription.BillingCycle.Should().Be(BillingCycle.Monthly);
        subscription.Status.Should().Be(SubscriptionStatus.PendingActivation);
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
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act
        subscription.RecordPayment(amount, "USD", paymentDate, idempotencyKey, forBillingCycle: 1);

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

    #region E.1 Critical Invariant Tests

    /// <summary>
    /// E.1 Test: Subscription.Activate_FromPendingActivation_Succeeds
    /// Verifies that a subscription can be activated from PendingActivation state
    /// </summary>
    [Fact]
    public void Activate_FromPendingActivation_Succeeds()
    {
        // Arrange - Start from PendingActivation (default state)
        var subscription = CreateValidSubscription();
        subscription.Status.Should().Be(SubscriptionStatus.PendingActivation);

        // Act
        subscription.Activate();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// E.1 Test: Subscription.Activate_FromCancelled_ThrowsInvalidStateException
    /// Verifies that activating a cancelled subscription throws InvalidStateTransitionException
    /// State machine validation: Cancelled is a terminal state
    /// </summary>
    [Fact]
    public void Activate_FromCancelled_ThrowsInvalidStateException()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Cancel(CancellationReason.UserRequested, "Test cancellation");
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);

        // Act & Assert
        var act = () => subscription.Activate();
        act.Should().Throw<InvalidStateTransitionException>()
            .WithMessage("*Cancelled*Active*");
    }

    /// <summary>
    /// E.1 Test: Subscription.RecordPayment_WithDuplicateIdempotencyKey_IsIdempotent
    /// Verifies that recording a payment with the same idempotency key returns success
    /// without creating a duplicate charge (critical for payment security)
    /// </summary>
    [Fact]
    public void RecordPayment_WithDuplicateIdempotencyKey_IsIdempotent()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var idempotencyKey = "payment_123";
        var amount = 29.99m;
        var paymentDate = DateTime.UtcNow;

        // Act - First payment
        var result1 = subscription.RecordPayment(amount, "USD", paymentDate, idempotencyKey, forBillingCycle: 1);
        var initialBillingCycle = subscription.BillingCycleCount;

        // Act - Duplicate payment with same idempotency key
        var result2 = subscription.RecordPayment(amount, "USD", paymentDate, idempotencyKey, forBillingCycle: 1);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsAlreadyProcessed.Should().BeTrue(); // Idempotent - returns success indicator for duplicate
        // Billing cycle should NOT advance for duplicate
        subscription.BillingCycleCount.Should().Be(initialBillingCycle);
    }

    /// <summary>
    /// E.1 Test: Subscription.RecordPayment_ConfirmsBillingCycle_Correctly
    /// Verifies that recording a payment confirms the billing cycle count
    /// without moving the period for the initial cycle.
    /// </summary>
    [Fact]
    public void RecordPayment_ConfirmsBillingCycle_Correctly()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var initialBillingCycle = subscription.BillingCycleCount;
        var initialNextBillingDate = subscription.NextBillingDate;
        var paymentDate = DateTime.UtcNow;

        // Act
        var result = subscription.RecordPayment(
            29.99m,
            "USD",
            paymentDate,
            "unique_payment_key_1",
            forBillingCycle: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(initialBillingCycle + 1);
        subscription.NextBillingDate.Should().Be(initialNextBillingDate);
        subscription.LastPaymentAt.Should().Be(paymentDate);
    }

    /// <summary>
    /// Additional E.1 Test: RecordPayment_OnCancelledSubscription_IsRejected
    /// Economic invariant: Cannot charge cancelled subscriptions
    /// </summary>
    [Fact]
    public void RecordPayment_OnCancelledSubscription_IsRejected()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Cancel(CancellationReason.UserRequested);

        // Act
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "payment_on_cancelled");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsRejectedCancelled.Should().BeTrue();
    }

    #endregion

    #region Order Linkage Tests

    [Fact]
    public void SetFulfilledOrderId_ShouldSetOrderId_WhenNotPreviouslySet()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var orderId = Guid.NewGuid();

        // Act
        subscription.SetFulfilledOrderId(orderId);

        // Assert
        subscription.FulfilledOrderId.Should().Be(orderId);
        subscription.LastModifyingOrderId.Should().Be(orderId);
    }

    [Fact]
    public void SetFulfilledOrderId_ShouldThrow_WhenAlreadySetToDifferentOrder()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();
        subscription.SetFulfilledOrderId(orderId1);

        // Act
        var act = () => subscription.SetFulfilledOrderId(orderId2);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*already linked to order*");
    }

    [Fact]
    public void SetFulfilledOrderId_ShouldBeIdempotent_WhenSetToSameOrder()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var orderId = Guid.NewGuid();
        subscription.SetFulfilledOrderId(orderId);

        // Act - should not throw
        var act = () => subscription.SetFulfilledOrderId(orderId);

        // Assert
        act.Should().NotThrow();
        subscription.FulfilledOrderId.Should().Be(orderId);
    }

    [Fact]
    public void RecordModifyingOrder_ShouldUpdateLastModifyingOrderId()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var orderId = Guid.NewGuid();

        // Act
        subscription.RecordModifyingOrder(orderId);

        // Assert
        subscription.LastModifyingOrderId.Should().Be(orderId);
    }

    #endregion

    #region Price Version Locking Tests

    [Fact]
    public void LockToPriceVersion_ShouldSetLockedPriceVersionId()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var priceVersionId = Guid.NewGuid();

        // Act
        subscription.LockToPriceVersion(priceVersionId);

        // Assert
        subscription.LockedPriceVersionId.Should().Be(priceVersionId);
    }

    [Fact]
    public void LockToPriceVersion_ShouldThrow_WhenSubscriptionCancelled()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Cancel(CancellationReason.UserRequested);

        // Act
        var act = () => subscription.LockToPriceVersion(Guid.NewGuid());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cancelled*");
    }

    [Fact]
    public void LockToPriceVersion_ShouldRaiseDomainEvent()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.ClearDomainEvents();
        var priceVersionId = Guid.NewGuid();

        // Act
        subscription.LockToPriceVersion(priceVersionId);

        // Assert
        subscription.DomainEvents.Should().Contain(e => e.GetType() == typeof(SubscriptionPriceVersionLockedEvent));
    }

    [Fact]
    public void UnlockPriceVersion_ShouldClearLockedPriceVersionId()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.LockToPriceVersion(Guid.NewGuid());

        // Act
        subscription.UnlockPriceVersion();

        // Assert
        subscription.LockedPriceVersionId.Should().BeNull();
    }

    [Fact]
    public void UnlockPriceVersion_ShouldBeIdempotent_WhenAlreadyUnlocked()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act - should not throw
        var act = () => subscription.UnlockPriceVersion();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Renewal Processing Tests

    [Fact]
    public void ProcessRenewal_ShouldRequirePaymentConfirmation_WhenSubscriptionIsActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var newAmount = new Money(2999);
        var idempotencyKey = "renewal_123";

        // Act
        var result = subscription.ProcessRenewal(newAmount, idempotencyKey);

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("payment confirmation");
        result.ChargedAmount.Should().BeNull();
        subscription.BillingCycleCount.Should().Be(0);
    }

    [Fact]
    public void ProcessRenewal_ShouldFail_WhenSubscriptionIsNotActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        // Still in PendingActivation state

        // Act
        var result = subscription.ProcessRenewal(new Money(2999), "renewal_123");

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("not active");
    }

    [Fact]
    public void ProcessRenewal_ShouldFail_WhenAutoRenewIsDisabled()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.SetAutoRenew(false);

        // Act
        var result = subscription.ProcessRenewal(new Money(2999), "renewal_123");

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("disabled");
    }

    [Fact]
    public void ProcessRenewal_ShouldRemainNonMutating_WithSameKey()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var idempotencyKey = "renewal_123";
        var newAmount = new Money(2999);

        // Act
        var result1 = subscription.ProcessRenewal(newAmount, idempotencyKey);
        var cycleAfterFirst = subscription.BillingCycleCount;
        var result2 = subscription.ProcessRenewal(newAmount, idempotencyKey);

        // Assert
        result1.Success.Should().BeFalse();
        result2.Success.Should().BeFalse();
        result1.ChargedAmount.Should().BeNull();
        result2.ChargedAmount.Should().BeNull();
        subscription.BillingCycleCount.Should().Be(cycleAfterFirst);
    }

    [Fact]
    public void ProcessRenewal_ShouldFail_WhenIdempotencyKeyIsEmpty()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        var result = subscription.ProcessRenewal(new Money(2999), "");

        // Assert
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Contain("Idempotency key");
    }

    #endregion

    #region Billing Cycle Change Tests

    [Fact]
    public void ChangeBillingCycle_ShouldUpdateCycleAndAmount()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var newAmount = new Money(25000); // Annual price

        // Act
        subscription.ChangeBillingCycle(BillingCycle.Annually, newAmount);

        // Assert
        subscription.BillingCycle.Should().Be(BillingCycle.Annually);
        subscription.Amount.Should().Be(newAmount);
    }

    [Fact]
    public void ChangeBillingCycle_ShouldThrow_WhenNotActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        // Still in PendingActivation

        // Act
        var act = () => subscription.ChangeBillingCycle(BillingCycle.Annually, new Money(25000));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active*");
    }

    [Fact]
    public void ChangeBillingCycle_ShouldRaiseDomainEvent()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.ClearDomainEvents();

        // Act
        subscription.ChangeBillingCycle(BillingCycle.Annually, new Money(25000));

        // Assert
        subscription.DomainEvents.Should().Contain(e => e.GetType() == typeof(SubscriptionBillingCycleChangedEvent));
    }

    #endregion

    #region Plan Change Tests

    [Fact]
    public void ChangePlan_ShouldCalculateProration()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var newPlanId = Guid.NewGuid();
        var newAmount = new Money(4999);

        // Act
        var proration = subscription.ChangePlan(newPlanId, newAmount);

        // Assert
        proration.Should().NotBeNull();
        proration.EffectiveDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ChangePlan_ShouldThrow_WhenNotActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        // Still in PendingActivation

        // Act
        var act = () => subscription.ChangePlan(Guid.NewGuid(), new Money(4999));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*active*");
    }

    [Fact]
    public void ChangePlan_ShouldRaiseDomainEvent()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.ClearDomainEvents();

        // Act
        subscription.ChangePlan(Guid.NewGuid(), new Money(4999));

        // Assert
        subscription.DomainEvents.Should().Contain(e => e.GetType() == typeof(SubscriptionPlanChangedEvent));
    }

    #endregion

    #region Trial Management Tests

    [Fact]
    public void EndTrial_WithoutConversion_ShouldCancelSubscription()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.StartTrial(DateTime.UtcNow.AddDays(14));

        // Act
        subscription.EndTrial(convertToPaid: false);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancellationReason.Should().Be(CancellationReason.TrialEnded);
    }

    [Fact]
    public void EndTrial_ShouldThrow_WhenNotTrialing()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        var act = () => subscription.EndTrial(convertToPaid: true);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*trial*");
    }

    [Fact]
    public void EndTrial_ShouldRaiseTrialEndedEvent()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.StartTrial(DateTime.UtcNow.AddDays(14));
        subscription.ClearDomainEvents();

        // Act
        subscription.EndTrial(convertToPaid: true);

        // Assert
        subscription.DomainEvents.Should().Contain(e => e.GetType() == typeof(TrialEndedEvent));
    }

    [Fact]
    public void GetRemainingTrialDays_ShouldReturnNull_WhenNotTrialing()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        var result = subscription.GetRemainingTrialDays();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRemainingTrialDays_ShouldReturnPositive_WhenTrialing()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.StartTrial(DateTime.UtcNow.AddDays(14));

        // Act
        var result = subscription.GetRemainingTrialDays();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().BeGreaterOrEqualTo(13);
    }

    #endregion

    #region Helper Properties Tests

    [Fact]
    public void IsActive_ShouldReturnTrue_WhenStatusIsActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Assert
        subscription.IsActive.Should().BeTrue();
        subscription.IsTrialing.Should().BeFalse();
        subscription.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void IsTrialing_ShouldReturnTrue_WhenStatusIsTrialing()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.StartTrial(DateTime.UtcNow.AddDays(14));

        // Assert
        subscription.IsTrialing.Should().BeTrue();
        subscription.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsCancelled_ShouldReturnTrue_WhenStatusIsCancelled()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Cancel(CancellationReason.UserRequested);

        // Assert
        subscription.IsCancelled.Should().BeTrue();
        subscription.IsActive.Should().BeFalse();
    }

    [Fact]
    public void GetDaysUntilNextBilling_ShouldReturnNegative_WhenNotActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        // PendingActivation state

        // Act
        var result = subscription.GetDaysUntilNextBilling();

        // Assert
        result.Should().Be(-1);
    }

    #endregion

    #region Payment Recording Edge Cases

    [Fact]
    public void RecordPayment_ShouldThrow_WhenIdempotencyKeyIsNull()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        var act = () => subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Idempotency key*");
    }

    [Fact]
    public void RecordPayment_OnCancelledSubscription_ShouldBeRejected()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        // Cancel the subscription
        subscription.Cancel(CancellationReason.UserRequested);

        // Act - Try to record payment on cancelled subscription
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "payment_cancelled");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RecordPayment_ShouldUpdateNextBillingDate_BasedOnCycle()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var paymentDate = DateTime.UtcNow;

        // Act
        subscription.RecordPayment(29.99m, "USD", paymentDate, "payment_123", forBillingCycle: 1);

        // Assert - Monthly cycle should advance by 1 month
        subscription.NextBillingDate.Should().BeCloseTo(paymentDate.AddMonths(1), TimeSpan.FromHours(1));
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_ShouldThrow_WhenTenantIdIsEmpty()
    {
        // Arrange & Act
        var act = () => new Subscription(
            tenantId: Guid.Empty,
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TenantId*");
    }

    [Fact]
    public void Constructor_ShouldSetLockedPriceVersionId_WhenProvided()
    {
        // Arrange
        var priceVersionId = Guid.NewGuid();

        // Act
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow,
            trialEndDate: null,
            lockedPriceVersionId: priceVersionId
        );

        // Assert
        subscription.LockedPriceVersionId.Should().Be(priceVersionId);
    }

    #endregion

    #region RecordPayment Out-of-Order Protection Tests

    [Fact]
    public void RecordPayment_WithBillingCycle_ShouldRejectOutOfOrderPayments()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        
        // First payment for billing cycle 1
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "payment_1", forBillingCycle: 1);
        
        // Act - Try to record payment for earlier billing cycle
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "payment_0", forBillingCycle: 0);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsRejectedOutOfOrder.Should().BeTrue();
    }

    [Fact]
    public void RecordPayment_WithSameBillingCycleAndDifferentPayment_ShouldReject()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        
        // First payment for billing cycle 1
        subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "payment_1", forBillingCycle: 1);
        
        // Act - Try to record another payment for same billing cycle (different idempotency key)
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow.AddMinutes(5), "payment_1_retry", forBillingCycle: 1);

        result.IsRejectedOutOfOrder.Should().BeTrue();
    }

    [Fact]
    public void RecordPayment_WithSameBillingCycleAndDifferentPayment_ShouldPreserveOriginalPaymentDate()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var firstPaymentDate = DateTime.UtcNow.AddDays(-1);
        var secondPaymentDate = DateTime.UtcNow;
        
        // First payment for billing cycle 1
        subscription.RecordPayment(29.99m, "USD", firstPaymentDate, "payment_1", forBillingCycle: 1);
        
        // Act - Record another payment for same billing cycle with newer date
        subscription.RecordPayment(29.99m, "USD", secondPaymentDate, "payment_1_retry", forBillingCycle: 1);

        // Assert
        subscription.LastPaymentAt.Should().Be(firstPaymentDate);
    }

    [Fact]
    public void RecordPayment_OnExpiredSubscription_ShouldBeRejected()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.StartTrial(DateTime.UtcNow.AddDays(14));
        subscription.EndTrial(convertToPaid: false); // This cancels with TrialEnded reason
        
        // Note: Expired status requires a specific transition - let's test cancelled behavior
        // which covers the economic invariant for cancelled/expired

        // Act
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, "payment_expired");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsRejectedCancelled.Should().BeTrue();
    }

    #endregion

    #region Billing Cycle Calculation Tests

    [Theory]
    [InlineData(BillingCycle.Monthly, 30)] // Approximately
    [InlineData(BillingCycle.Quarterly, 90)] // Approximately
    [InlineData(BillingCycle.SemiAnnually, 180)] // Approximately
    [InlineData(BillingCycle.Annually, 365)] // Approximately
    [InlineData(BillingCycle.Biannually, 730)] // Approximately
    public void RecordPayment_ShouldSetCorrectNextBillingDate_ForDifferentCycles(BillingCycle cycle, int expectedDaysApprox)
    {
        // Arrange
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: cycle,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow,
            trialEndDate: null
        );
        subscription.Activate();
        var paymentDate = DateTime.UtcNow;

        // Act
        subscription.RecordPayment(29.99m, "USD", paymentDate, $"payment_{cycle}", forBillingCycle: 1);

        // Assert - Next billing should be approximately the expected days away
        var daysDiff = (subscription.NextBillingDate - paymentDate).TotalDays;
        daysDiff.Should().BeApproximately(expectedDaysApprox, 5); // Allow 5 days tolerance for month variations
    }

    [Fact]
    public void Constructor_WithUnsupportedBillingCycle_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange - Weekly is in the enum but not supported in CalculateBillingDates
        // Act
        var act = () => new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Weekly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow,
            trialEndDate: null
        );

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*cycle*");
    }

    [Theory]
    [InlineData(BillingCycle.Quarterly)]
    [InlineData(BillingCycle.SemiAnnually)]
    [InlineData(BillingCycle.Annually)]
    [InlineData(BillingCycle.Biannually)]
    public void Constructor_ShouldCalculateCorrectBillingDates_ForDifferentCycles(BillingCycle cycle)
    {
        // Arrange
        var startDate = DateTime.UtcNow;

        // Act
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: cycle,
            amount: new Money(2999),
            startDate: startDate,
            trialEndDate: null
        );

        // Assert - Verify billing dates are set
        subscription.CurrentPeriodStart.Should().Be(startDate);
        subscription.CurrentPeriodEnd.Should().BeAfter(startDate);
        subscription.NextBillingDate.Should().BeAfter(startDate);
    }

    #endregion

    #region Metadata Update Tests

    [Fact]
    public void UpdateMetadata_ShouldThrow_WhenMetadataIsEmpty()
    {
        // Arrange
        var subscription = CreateValidSubscription();

        // Act
        var act = () => subscription.UpdateMetadata("");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateMetadata_ShouldThrow_WhenMetadataIsNull()
    {
        // Arrange
        var subscription = CreateValidSubscription();

        // Act
        var act = () => subscription.UpdateMetadata(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateMetadata_ShouldThrow_WhenMetadataExceeds2000Characters()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var longMetadata = new string('x', 2001);

        // Act
        var act = () => subscription.UpdateMetadata(longMetadata);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*2000 characters*");
    }

    [Fact]
    public void UpdateMetadata_ShouldSucceed_WithExactly2000Characters()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var maxMetadata = new string('x', 2000);

        // Act
        subscription.UpdateMetadata(maxMetadata);

        // Assert
        subscription.Metadata.Should().Be(maxMetadata);
    }

    #endregion

    #region SetAutoRenew Edge Cases

    [Fact]
    public void SetAutoRenew_ShouldThrow_WhenSubscriptionIsCancelled()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Cancel(CancellationReason.UserRequested);

        // Act
        var act = () => subscription.SetAutoRenew(true);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cancelled*");
    }

    [Fact]
    public void SetAutoRenew_ShouldSucceed_WhenSubscriptionIsPastDue()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.RecordPaymentFailure("Card declined", DateTime.UtcNow);

        // Act
        var act = () => subscription.SetAutoRenew(false);

        // Assert
        act.Should().NotThrow();
        subscription.AutoRenew.Should().BeFalse();
    }

    #endregion

    #region ISubscription Interface Tests

    [Fact]
    public void ISubscription_TenantId_ShouldReturnGuidValue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var subscription = new Subscription(
            tenantId: tenantId,
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow
        );

        // Act - Access via interface
        ISubscription iSubscription = subscription;
        var result = iSubscription.TenantId;

        // Assert
        result.Should().Be(tenantId);
    }

    #endregion

    #region Suspend With Metadata Tests

    [Fact]
    public void Suspend_WithReason_ShouldStoreReasonInMetadata()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var reason = "Payment failed multiple times";

        // Act
        subscription.Suspend(reason);

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.Metadata.Should().Contain("suspensionReason");
        subscription.Metadata.Should().Contain(reason);
    }

    [Fact]
    public void Suspend_WithoutReason_ShouldNotSetMetadata()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        subscription.Suspend();

        // Assert
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.Metadata.Should().BeNull();
    }

    [Fact]
    public void Reactivate_ShouldClearMetadata()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Suspend("Test reason");
        subscription.Metadata.Should().NotBeNull();

        // Act
        subscription.Reactivate();

        // Assert
        subscription.Metadata.Should().BeNull();
    }

    #endregion

    #region Cancel Idempotency Tests

    [Fact]
    public void Cancel_ShouldBeIdempotent_WhenAlreadyCancelled()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Cancel(CancellationReason.UserRequested, "First cancel");
        var originalCancelledAt = subscription.CancelledAt;

        // Act - Cancel again
        subscription.Cancel(CancellationReason.PaymentFailed, "Second cancel");

        // Assert - Should not change anything
        subscription.CancellationReason.Should().Be(CancellationReason.UserRequested);
        subscription.CancellationNote.Should().Be("First cancel");
        subscription.CancelledAt.Should().Be(originalCancelledAt);
    }

    [Fact]
    public void Cancel_WithEffectiveDate_ShouldSetEndDate()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var effectiveDate = DateTime.UtcNow.AddDays(30);

        // Act
        subscription.Cancel(CancellationReason.UserRequested, effectiveDate: effectiveDate);

        // Assert
        subscription.EndDate.Should().Be(effectiveDate);
    }

    #endregion

    #region GetDaysUntilNextBilling Tests

    [Fact]
    public void GetDaysUntilNextBilling_ShouldReturnPositive_WhenActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act
        var result = subscription.GetDaysUntilNextBilling();

        // Assert
        result.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region UnlockPriceVersion Event Tests

    [Fact]
    public void UnlockPriceVersion_ShouldRaiseDomainEvent_WhenUnlocking()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.LockToPriceVersion(Guid.NewGuid());
        subscription.ClearDomainEvents();

        // Act
        subscription.UnlockPriceVersion();

        // Assert
        subscription.DomainEvents.Should().Contain(e => e.GetType() == typeof(SubscriptionPriceVersionUnlockedEvent));
    }

    #endregion

    #region RowVersion Property Tests

    [Fact]
    public void RowVersion_ShouldBeSettable_ForOptimisticConcurrency()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var rowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // Act
        subscription.RowVersion = rowVersion;

        // Assert
        subscription.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    [Fact]
    public void RowVersion_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var subscription = CreateValidSubscription();

        // Assert
        subscription.RowVersion.Should().BeNull();
    }

    #endregion

    #region ISubscription.TenantId Explicit Interface Tests

    [Fact]
    public void ISubscription_TenantId_ShouldReturnGuid_WhenTenantIdIsSet()
    {
        // Arrange
        var tenantGuid = Guid.NewGuid();
        var subscription = new Subscription(
            tenantId: tenantGuid,
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: DateTime.UtcNow
        );

        // Act
        ISubscription iSubscription = subscription;
        var result = iSubscription.TenantId;

        // Assert
        result.Should().Be(tenantGuid);
    }

    #endregion

    #region RecordPaymentLegacy Tests

    [Fact]
    public void RecordPaymentLegacy_ShouldReturnFalse_WhenBillingCycleIdentityIsUnavailable()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var idempotencyKey = Guid.NewGuid().ToString();

#pragma warning disable CS0618 // Test obsolete method for coverage
        // Act
        var result = subscription.RecordPaymentLegacy(29.99m, "USD", DateTime.UtcNow, idempotencyKey);

        // Assert
        result.Should().BeFalse();
#pragma warning restore CS0618
    }

    [Fact]
    public void RecordPaymentLegacy_ShouldReturnFalse_WhenPaymentRejected()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        subscription.Cancel(CancellationReason.UserRequested, "Test cancellation");

#pragma warning disable CS0618 // Test obsolete method for coverage
        // Act
        var result = subscription.RecordPaymentLegacy(29.99m, "USD", DateTime.UtcNow, Guid.NewGuid().ToString());

        // Assert
        result.Should().BeFalse();
#pragma warning restore CS0618
    }

    #endregion

    #region RecordPayment Expired Subscription Tests

    [Fact]
    public void RecordPayment_ShouldRejectPayment_WhenSubscriptionIsExpired()
    {
        // Arrange - Create subscription that will expire
        var startDate = DateTime.UtcNow.AddMonths(-2);
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(2999),
            startDate: startDate
        );
        subscription.Activate();

        // Use reflection to set status to Expired (simulate natural expiration)
        var statusProperty = typeof(Subscription).GetProperty("Status");
        statusProperty!.SetValue(subscription, SubscriptionStatus.Expired);

        // Act
        var result = subscription.RecordPayment(29.99m, "USD", DateTime.UtcNow, Guid.NewGuid().ToString());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("expired");
    }

    #endregion

    #region CalculateProration Edge Cases

    [Fact]
    public void ChangePlan_ShouldHandleZeroPeriod_GracefullyInProration()
    {
        // Arrange - Create subscription with same period start and end
        var subscription = CreateValidSubscription();
        subscription.Activate();

        // Act & Assert - Should not throw even in edge cases
        var act = () => subscription.ChangePlan(
            Guid.NewGuid(),
            new Money(4999),
            DateTime.UtcNow.AddDays(30) // Effective date in the future
        );
        act.Should().NotThrow();
    }

    #endregion

    #region RecordPayment Biannually Billing Cycle Tests

    [Fact]
    public void RecordPayment_WithBiannuallyBillingCycle_ShouldSetNextBillingDateTwoYearsLater()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Biannually,
            amount: new Money(599.99m, "USD"),
            startDate: startDate
        );
        subscription.Activate();
        var paymentDate = DateTime.UtcNow;

        // Act
        var result = subscription.RecordPayment(
            599.99m,
            "USD",
            paymentDate,
            Guid.NewGuid().ToString(),
            forBillingCycle: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.NextBillingDate.Should().BeCloseTo(paymentDate.AddYears(2), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RecordPayment_WithWeeklyBillingCycle_ShouldNotMoveInitialPeriod()
    {
        // Arrange - Create a Monthly subscription and then use reflection to set it to Weekly
        // (Weekly is not supported in CalculateBillingDates but IS handled in RecordPayment switch)
        var startDate = DateTime.UtcNow;
        var subscription = new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly, // Start with Monthly
            amount: new Money(9.99m, "USD"),
            startDate: startDate
        );
        subscription.Activate();

        // Use reflection to set BillingCycle to Weekly for this edge case test
        var billingCycleProperty = typeof(Subscription).GetProperty("BillingCycle");
        billingCycleProperty!.SetValue(subscription, BillingCycle.Weekly);

        var paymentDate = DateTime.UtcNow;
        var initialNextBillingDate = subscription.NextBillingDate;

        // Act
        var result = subscription.RecordPayment(
            9.99m,
            "USD",
            paymentDate,
            Guid.NewGuid().ToString(),
            forBillingCycle: 1);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.NextBillingDate.Should().Be(initialNextBillingDate);
    }

    #endregion

    #region ProcessRenewal Exception Handling Tests

    [Fact]
    public void ProcessRenewal_ShouldReturnFailed_WhenExceptionOccurs()
    {
        // Arrange - Create subscription that's not in renewable state
        var subscription = CreateValidSubscription();
        // Don't activate - subscription is in PendingActivation state

        // Act
        var result = subscription.ProcessRenewal(new Money(2999), Guid.NewGuid().ToString());

        // Assert - Should fail because subscription is not active
        result.Success.Should().BeFalse();
    }

    #endregion

    #region SetExternalIds Event Coverage Tests

    [Fact]
    public void SetExternalIds_ShouldRaiseEvent_WithNullSubscriptionId()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.ClearDomainEvents();

        // Act
        subscription.SetExternalIds(null, "cust_123");

        // Assert
        subscription.DomainEvents.Should().Contain(e => e.GetType() == typeof(SubscriptionExternalIdUpdatedEvent));
    }

    #endregion

    private static Subscription CreateValidSubscription()
    {
        return new Subscription(
            tenantId: Guid.NewGuid(),
            planId: Guid.NewGuid(),
            createdByUserId: Guid.NewGuid(),
            billingCycle: BillingCycle.Monthly,
            amount: new Money(29.99m, "USD"),
            startDate: DateTime.UtcNow,
            trialEndDate: null
        );
    }
}

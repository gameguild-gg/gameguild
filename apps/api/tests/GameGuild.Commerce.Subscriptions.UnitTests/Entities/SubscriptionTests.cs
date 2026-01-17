using GameGuild.ValueObjects;
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
        subscription.RecordPayment(amount, "USD", paymentDate, idempotencyKey);

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
        act.Should().Throw<GameGuild.SharedKernel.InvalidStateTransitionException>()
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
        var result1 = subscription.RecordPayment(amount, "USD", paymentDate, idempotencyKey);
        var initialBillingCycle = subscription.BillingCycleCount;

        // Act - Duplicate payment with same idempotency key
        var result2 = subscription.RecordPayment(amount, "USD", paymentDate, idempotencyKey);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsAlreadyProcessed.Should().BeTrue(); // Idempotent - returns success indicator for duplicate
        // Billing cycle should NOT advance for duplicate
        subscription.BillingCycleCount.Should().Be(initialBillingCycle);
    }

    /// <summary>
    /// E.1 Test: Subscription.RecordPayment_AdvancesBillingCycle_Correctly
    /// Verifies that recording a payment advances the billing cycle count
    /// and updates the next billing date appropriately
    /// </summary>
    [Fact]
    public void RecordPayment_AdvancesBillingCycle_Correctly()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var initialBillingCycle = subscription.BillingCycleCount;
        var initialNextBillingDate = subscription.NextBillingDate;
        var paymentDate = DateTime.UtcNow;

        // Act
        var result = subscription.RecordPayment(29.99m, "USD", paymentDate, "unique_payment_key_1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscription.BillingCycleCount.Should().Be(initialBillingCycle + 1);
        subscription.NextBillingDate.Should().BeAfter(initialNextBillingDate);
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
    public void ProcessRenewal_ShouldSucceed_WhenSubscriptionIsActive()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        subscription.Activate();
        var newAmount = new Money(2999);
        var idempotencyKey = "renewal_123";

        // Act
        var result = subscription.ProcessRenewal(newAmount, idempotencyKey);

        // Assert
        result.Success.Should().BeTrue();
        subscription.BillingCycleCount.Should().BeGreaterThan(0);
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
    public void ProcessRenewal_ShouldBeIdempotent_WithSameKey()
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
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue(); // Idempotent
        subscription.BillingCycleCount.Should().Be(cycleAfterFirst); // Not incremented again
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
        subscription.Cancel(CancellationReason.UserCancelled);

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
        subscription.RecordPayment(29.99m, "USD", paymentDate, "payment_123");

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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GameGuild;
using GameGuild.Database;
using GameGuild.Modules.Products;
using GameGuild.Modules.Subscriptions.Events;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Subscriptions.Models;

/// <summary>
/// Represents a user's subscription to a service plan with billing and lifecycle management.
/// Tracks subscription status, billing cycles, trial periods, and external payment provider integration.
/// Supports various subscription models including trials, recurring billing, and cancellations.
/// </summary>
[Table("user_subscriptions")]
[Index(nameof(UserId))]
[Index(nameof(Status))]
[Index(nameof(SubscriptionPlanId))]
[Index(nameof(CurrentPeriodStart))]
[Index(nameof(CurrentPeriodEnd))]
[Index(nameof(NextBillingAt))]
[Index(nameof(ExternalSubscriptionId))]
public class UserSubscription : EntityBase {
  /// <summary> The user who owns this subscription </summary>
  public Guid UserId { get; set; }

  /// <summary> The subscription plan that defines the service features and pricing </summary>
  public Guid SubscriptionPlanId { get; set; }

  /// <summary> 
  /// Current status of the subscription (Active, Trialing, Cancelled, etc.)
  /// Determines billing behavior and feature access
  /// </summary>
  public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

  /// <summary> 
  /// Billing cycle frequency that determines how often the user is charged.
  /// Common values: Monthly, Yearly, Weekly
  /// </summary>
  public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

  /// <summary> 
  /// Current subscription amount including any discounts or adjustments.
  /// Amount may change during subscription lifetime due to plan changes or prorations.
  /// </summary>
  public Money Amount { get; set; } = Money.Zero();

  /// <summary> 
  /// Whether this subscription automatically renews at the end of each billing period.
  /// When false, subscription will end at the current period end date.
  /// </summary>
  public bool AutoRenew { get; set; } = true;

  /// <summary> 
  /// Number of completed billing cycles since subscription creation.
  /// Used for tracking subscription tenure and applying cycle-based discounts.
  /// </summary>
  public int BillingCycleCount { get; set; }

  /// <summary> Reason for cancellation (if cancelled) </summary>
  public CancellationReason? CancellationReason { get; set; }

  /// <summary> Additional notes about cancellation </summary>
  [MaxLength(1000)]
  public string? CancellationNote { get; set; }

  /// <summary> External customer ID for payment provider </summary>
  [MaxLength(100)]
  public string? ExternalCustomerId { get; set; }

  /// <summary> Additional metadata (JSON serialized) </summary>
  [MaxLength(2000)]
  public string? Metadata { get; set; }

  /// <summary> External subscription ID from payment provider (Stripe, PayPal, etc.) </summary>
  [MaxLength(255)]
  public string? ExternalSubscriptionId { get; set; }

  /// <summary> Current billing period start date </summary>
  public DateTime CurrentPeriodStart { get; set; }

  /// <summary> Current billing period end date </summary>
  public DateTime CurrentPeriodEnd { get; set; }

  /// <summary> Date when the subscription was canceled (null if not canceled) </summary>
  public DateTime? CanceledAt { get; set; }

  /// <summary> Date when the subscription will end (null if indefinite) </summary>
  public DateTime? EndsAt { get; set; }

  /// <summary> Date when the trial period ends (null if no trial) </summary>
  public DateTime? TrialEndsAt { get; set; }

  /// <summary> Last successful payment date </summary>
  public DateTime? LastPaymentAt { get; set; }

  /// <summary> Next scheduled billing date </summary>
  public DateTime? NextBillingAt { get; set; }

  // Navigation properties
  [ForeignKey(nameof(UserId))] public virtual User User { get; set; } = null!;

  [ForeignKey(nameof(SubscriptionPlanId))] public virtual ProductSubscriptionPlan SubscriptionPlan { get; set; } = null!;

  public virtual ICollection<UserProduct> UserProducts { get; set; } = new List<UserProduct>();

  // Domain Properties

  /// <summary> Checks if the subscription is currently active </summary>
  public bool IsActive { get => Status == SubscriptionStatus.Active; }

  /// <summary> Checks if the subscription is in trial </summary>
  public bool IsTrialing { get => Status == SubscriptionStatus.Trialing; }

  /// <summary> Checks if the subscription is cancelled </summary>
  public bool IsCancelled { get => Status == SubscriptionStatus.Cancelled; }

  /// <summary> Gets remaining trial days (if in trial) </summary>
  public int? GetRemainingTrialDays() {
    if (!IsTrialing || !TrialEndsAt.HasValue) return null;

    // Calculate days remaining, ensuring we use UTC for consistency
    var remaining = (TrialEndsAt.Value - DateTime.UtcNow).Days;

    // Prevent negative values for expired trials
    return Math.Max(0, remaining);
  }

  /// <summary> Gets days until next billing </summary>
  public int GetDaysUntilNextBilling() {
    // Return -1 for inactive subscriptions or missing billing dates
    if (!IsActive || !NextBillingAt.HasValue) return -1;

    // Calculate days until billing, preventing negative values
    return Math.Max(0, (NextBillingAt.Value - DateTime.UtcNow).Days);
  }

  // Domain Methods

  /// <summary> Activates the subscription </summary>
  public void Activate() {
    // Only allow activation from specific states to maintain data integrity
    if (Status != SubscriptionStatus.PendingActivation && Status != SubscriptionStatus.Trialing)
      throw new InvalidOperationException("Can only activate pending or trialing subscriptions");

    Status = SubscriptionStatus.Active;
    Touch(); // Update timestamps
    AddDomainEvent(new SubscriptionActivatedEvent(Id, UserId));
  }

  /// <summary> Starts a trial period </summary>
  public void StartTrial(DateTime trialEndDate) {
    if (Status != SubscriptionStatus.PendingActivation) throw new InvalidOperationException("Can only start trial for pending subscriptions");

    TrialEndsAt = trialEndDate;
    Status = SubscriptionStatus.Trialing;
    Touch();
    AddDomainEvent(new SubscriptionTrialStartedEvent(Id, UserId, trialEndDate));
  }

  /// <summary> Ends the trial period </summary>
  public void EndTrial(bool convertToPaid) {
    if (Status != SubscriptionStatus.Trialing) throw new InvalidOperationException("Can only end trial for trialing subscriptions");

    if (convertToPaid) {
      // Convert trial to active paid subscription
      Status = SubscriptionStatus.Active;
      AddDomainEvent(new SubscriptionActivatedEvent(Id, UserId));
    }
    else {
      // Cancel subscription if user doesn't convert to paid
      Cancel(Models.CancellationReason.TrialEnded, "Trial period ended without conversion");
    }

    Touch();
    AddDomainEvent(new SubscriptionTrialEndedEvent(Id, UserId, convertToPaid));
  }

  /// <summary> Cancels the subscription </summary>
  public void Cancel(CancellationReason reason, string? note = null, DateTime? effectiveDate = null) {
    // Idempotent operation - no-op if already cancelled
    if (Status == SubscriptionStatus.Cancelled) return;

    var oldStatus = Status; // Preserve for domain event
    Status = SubscriptionStatus.Cancelled;
    CancellationReason = reason;
    CancellationNote = note;
    CanceledAt = DateTime.UtcNow;

    // Use provided effective date or immediate cancellation
    EndsAt = effectiveDate ?? DateTime.UtcNow;

    // Prevent future renewals
    AutoRenew = false;

    Touch();
    AddDomainEvent(new SubscriptionCancelledEvent(Id, UserId, reason.ToString(), oldStatus));
  }

  /// <summary> Suspends the subscription temporarily </summary>
  public void Suspend(string? reason = null) {
    if (Status != SubscriptionStatus.Active) throw new InvalidOperationException("Can only suspend active subscriptions");

    Status = SubscriptionStatus.Suspended;
    AutoRenew = false;

    if (!string.IsNullOrEmpty(reason)) { Metadata = JsonSerializer.Serialize(new { suspensionReason = reason }); }

    Touch();
    AddDomainEvent(new SubscriptionSuspendedEvent(Id, UserId, reason));
  }

  /// <summary> Updates the subscription amount </summary>
  public void UpdateAmount(Money newAmount) {
    if (newAmount.Amount < 0) throw new ArgumentException("Amount cannot be negative");

    Amount = newAmount;
    Touch();
  }

  /// <summary> Increments the billing cycle count </summary>
  public void IncrementBillingCycle() {
    BillingCycleCount++;
    Touch();
  }

  /// <summary> Records a successful payment </summary>
  public void RecordPayment(DateTime paymentDate) {
    LastPaymentAt = paymentDate;

    // Calculate next billing date based on billing cycle
    // Default to monthly if billing cycle is unrecognized
    NextBillingAt = BillingCycle switch {
      BillingCycle.Monthly => paymentDate.AddMonths(1),
      BillingCycle.Quarterly => paymentDate.AddMonths(3),
      BillingCycle.SemiAnnually => paymentDate.AddMonths(6),
      BillingCycle.Annually => paymentDate.AddYears(1),
      BillingCycle.Biannually => paymentDate.AddYears(2),
      _ => paymentDate.AddMonths(1), // Fallback to monthly
    };

    // Track billing cycles for analytics and business logic
    IncrementBillingCycle();
  }

  /// <summary> Factory method to create a new subscription </summary>
  public static UserSubscription Create(Guid userId, Guid subscriptionPlanId, BillingCycle billingCycle, Money amount, DateTime startDate, DateTime? trialEndDate = null) {
    var subscription = new UserSubscription {
      UserId = userId,
      SubscriptionPlanId = subscriptionPlanId,
      BillingCycle = billingCycle,
      Amount = amount,
      CurrentPeriodStart = startDate,
      // Set initial status based on trial configuration
      Status = trialEndDate.HasValue ? SubscriptionStatus.Trialing : SubscriptionStatus.PendingActivation,
    };

    // Configure trial period if specified
    if (trialEndDate.HasValue) { subscription.TrialEndsAt = trialEndDate; }

    // Calculate billing period end date based on cycle type
    // Default to monthly for unrecognized cycles
    subscription.CurrentPeriodEnd = billingCycle switch {
      BillingCycle.Monthly => startDate.AddMonths(1),
      BillingCycle.Quarterly => startDate.AddMonths(3),
      BillingCycle.SemiAnnually => startDate.AddMonths(6),
      BillingCycle.Annually => startDate.AddYears(1),
      BillingCycle.Biannually => startDate.AddYears(2),
      _ => startDate.AddMonths(1), // Fallback to monthly
    };

    // Set initial billing date to period end
    subscription.NextBillingAt = subscription.CurrentPeriodEnd;

    // Raise domain event for subscription creation
    subscription.AddDomainEvent(new SubscriptionCreatedEvent(subscription.Id, userId, subscriptionPlanId, startDate, trialEndDate));

    return subscription;
  }
}

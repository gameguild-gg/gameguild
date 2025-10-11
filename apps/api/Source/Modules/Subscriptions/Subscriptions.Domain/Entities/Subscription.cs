using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Events;
using GameGuild.Modules.Subscriptions.Models;
using GameGuild.Modules.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Subscriptions.Entities;

/// <summary>
///     Represents a subscription linking a tenant to a subscription plan
/// </summary>
[Table("Subscriptions")]
[Index(nameof(TenantId), nameof(Status))]
[Index(nameof(PlanId))]
[Index(nameof(NextBillingDate))]
[Index(nameof(Status))]
public class Subscription : EntityBase, ISubscription
{
    /// <summary>
    ///     Default constructor for EF
    /// </summary>
    private Subscription() { }

    /// <summary>
    ///     Creates a new subscription
    /// </summary>
    public Subscription(
        Guid tenantId,
        Guid planId,
        Guid createdByUserId,
        BillingCycle billingCycle,
        Money amount,
        DateTime startDate,
        DateTime? trialEndDate = null)
    {
        TenantId = tenantId;
        PlanId = planId;
        CreatedByUserId = createdByUserId;
        BillingCycle = billingCycle;
        Amount = amount;
        StartDate = startDate;
        TrialEndDate = trialEndDate;
        Status = trialEndDate.HasValue ? SubscriptionStatus.Trialing : SubscriptionStatus.PendingActivation;

        (DateTime periodStart, DateTime periodEnd, DateTime nextBilling) = CalculateBillingDates(startDate, billingCycle);
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        NextBillingDate = nextBilling;

        Raise(new SubscriptionCreatedEvent(Id, tenantId, planId));
    }

    /// <summary>
    ///     Reference to the user who created/manages this subscription
    /// </summary>
    [Required]
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    ///     Trial end date (if applicable)
    /// </summary>
    public DateTime? TrialEndDate { get; private set; }

    /// <summary>
    ///     Reason for cancellation (if cancelled)
    /// </summary>
    public CancellationReason? CancellationReason { get; private set; }

    /// <summary>
    ///     Additional notes about cancellation
    /// </summary>
    [MaxLength(1000)]
    public string? CancellationNote { get; private set; }

    /// <summary>
    ///     When the subscription was cancelled
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    ///     External ID for payment provider integration (Stripe subscription ID, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? ExternalId { get; private set; }

    /// <summary>
    ///     External customer ID for payment provider
    /// </summary>
    [MaxLength(100)]
    public string? ExternalCustomerId { get; private set; }

    /// <summary>
    ///     Whether this subscription auto-renews
    /// </summary>
    public bool AutoRenew { get; private set; } = true;

    /// <summary>
    ///     Current billing period start
    /// </summary>
    public DateTime CurrentPeriodStart { get; private set; }

    /// <summary>
    ///     Current billing period end
    /// </summary>
    public DateTime CurrentPeriodEnd { get; private set; }

    /// <summary>
    ///     Number of billing cycles processed
    /// </summary>
    public int BillingCycleCount { get; private set; }

    /// <summary>
    ///     Additional metadata (JSON serialized)
    /// </summary>
    [MaxLength(2000)]
    public string? Metadata { get; private set; }

    // Navigation properties
    public override virtual Tenant Tenant { get; set; } = null!;

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    /// <summary>
    ///     Current status of the subscription
    /// </summary>
    public SubscriptionStatus Status { get; private set; }

    /// <summary>
    ///     Reference to the tenant that owns this subscription
    /// </summary>
    [Required]
    public Guid TenantId { get; }

    /// <summary>
    ///     Reference to the subscription plan
    /// </summary>
    [Required]
    public Guid PlanId { get; private set; }

    /// <summary>
    ///     Billing cycle frequency
    /// </summary>
    public BillingCycle BillingCycle { get; private set; }

    /// <summary>
    ///     Current subscription amount
    /// </summary>
    public Money Amount { get; private set; } = Money.Zero();

    /// <summary>
    ///     When the subscription started/became active
    /// </summary>
    public DateTime StartDate { get; }

    /// <summary>
    ///     When the subscription ends (null for active subscriptions)
    /// </summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>
    ///     When the next billing cycle occurs
    /// </summary>
    public DateTime NextBillingDate { get; private set; }

    /// <summary>
    ///     Checks if the subscription is currently active
    /// </summary>
    public bool IsActive
    {
        get => Status == SubscriptionStatus.Active;
    }

    /// <summary>
    ///     Checks if the subscription is in trial
    /// </summary>
    public bool IsTrialing
    {
        get => Status == SubscriptionStatus.Trialing;
    }

    /// <summary>
    ///     Checks if the subscription is cancelled
    /// </summary>
    public bool IsCancelled
    {
        get => Status == SubscriptionStatus.Cancelled;
    }

    /// <summary>
    ///     Gets remaining trial days (if in trial)
    /// </summary>
    public int? GetRemainingTrialDays()
    {
        if (!IsTrialing || !TrialEndDate.HasValue)
            return null;

        int remaining = (TrialEndDate.Value - DateTime.UtcNow).Days;

        return Math.Max(0, remaining);
    }

    /// <summary>
    ///     Gets days until next billing
    /// </summary>
    public int GetDaysUntilNextBilling()
    {
        if (!IsActive) return -1;

        return Math.Max(0, (NextBillingDate - DateTime.UtcNow).Days);
    }

    /// <summary>
    ///     Activates the subscription
    /// </summary>
    public void Activate()
    {
        if (Status != SubscriptionStatus.PendingActivation && Status != SubscriptionStatus.Trialing)
            throw new InvalidOperationException("Can only activate pending or trialing subscriptions");

        Status = SubscriptionStatus.Active;
        Raise(new SubscriptionActivatedEvent(Id, TenantId));
    }

    /// <summary>
    ///     Starts a trial period
    /// </summary>
    public void StartTrial(DateTime trialEndDate)
    {
        if (Status != SubscriptionStatus.PendingActivation)
            throw new InvalidOperationException("Can only start trial for pending subscriptions");

        TrialEndDate = trialEndDate;
        Status = SubscriptionStatus.Trialing;
    }

    /// <summary>
    ///     Ends the trial period
    /// </summary>
    public void EndTrial(bool convertToPaid)
    {
        if (Status != SubscriptionStatus.Trialing)
            throw new InvalidOperationException("Can only end trial for trialing subscriptions");

        if (convertToPaid)
        {
            Status = SubscriptionStatus.Active;
            Raise(new SubscriptionActivatedEvent(Id, TenantId));
        }
        else
        {
            var reason = ModuState.Subscriptions.Domain.Models.CancellationReason.TrialEnded;
            Cancel(reason, "Trial period ended without conversion");
        }
    }

    /// <summary>
    ///     Cancels the subscription
    /// </summary>
    public void Cancel(CancellationReason reason, string? note = null, DateTime? effectiveDate = null)
    {
        if (Status == SubscriptionStatus.Cancelled)
            return;

        SubscriptionStatus oldStatus = Status;
        Status = SubscriptionStatus.Cancelled;
        CancellationReason = reason;
        CancellationNote = note;
        CancelledAt = DateTime.UtcNow;
        EndDate = effectiveDate ?? DateTime.UtcNow;
        AutoRenew = false;

        Raise(new SubscriptionCancelledEvent(Id, TenantId, reason, oldStatus));
    }

    /// <summary>
    ///     Suspends the subscription temporarily
    /// </summary>
    public void Suspend(string? reason = null)
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Can only suspend active subscriptions");

        Status = SubscriptionStatus.Suspended;
        AutoRenew = false;

        if (!string.IsNullOrEmpty(reason))
        {
            Metadata = JsonSerializer.Serialize(
                new
                {
                    suspensionReason = reason
                }
            );
        }

        Raise(new SubscriptionSuspendedEvent(Id, TenantId, reason));
    }

    /// <summary>
    ///     Reactivates a suspended subscription
    /// </summary>
    public void Reactivate()
    {
        if (Status != SubscriptionStatus.Suspended)
            throw new InvalidOperationException("Can only reactivate suspended subscriptions");

        Status = SubscriptionStatus.Active;
        AutoRenew = true;
        Metadata = null;

        Raise(new SubscriptionReactivatedEvent(Id, TenantId));
    }

    /// <summary>
    ///     Updates the subscription plan
    /// </summary>
    public void ChangePlan(Guid newPlanId, Money newAmount, DateTime? effectiveDate = null)
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Can only change plans for active subscriptions");

        Guid oldPlanId = PlanId;
        var oldAmount = Amount;

        PlanId = newPlanId;
        Amount = newAmount;

        Raise(new SubscriptionPlanChangedEvent(Id, TenantId, oldPlanId, newPlanId, oldAmount, newAmount));
    }

    /// <summary>
    ///     Updates the billing cycle
    /// </summary>
    public void ChangeBillingCycle(BillingCycle newBillingCycle, Money newAmount)
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Can only change billing cycle for active subscriptions");

        BillingCycle oldCycle = BillingCycle;
        var oldAmount = Amount;

        BillingCycle = newBillingCycle;
        Amount = newAmount;

        (DateTime periodStart, DateTime periodEnd, DateTime nextBilling) = CalculateBillingDates(CurrentPeriodStart, newBillingCycle);
        CurrentPeriodEnd = periodEnd;
        NextBillingDate = nextBilling;

        Raise(new SubscriptionBillingCycleChangedEvent(Id, TenantId, oldCycle, newBillingCycle, oldAmount, newAmount));
    }

    /// <summary>
    ///     Processes a renewal (moves to next billing period)
    /// </summary>
    public SubscriptionRenewalResult ProcessRenewal(Money newAmount)
    {
        if (Status != SubscriptionStatus.Active)
            return SubscriptionRenewalResult.Failed(Id, "Subscription is not active");

        if (!AutoRenew)
            return SubscriptionRenewalResult.Failed(Id, "Auto-renewal is disabled");

        try
        {
            Amount = newAmount;
            BillingCycleCount++;

            (DateTime periodStart, DateTime periodEnd, DateTime nextBilling) = CalculateBillingDates(NextBillingDate, BillingCycle);
            CurrentPeriodStart = periodStart;
            CurrentPeriodEnd = periodEnd;
            NextBillingDate = nextBilling;

            Raise(new SubscriptionRenewedEvent(Id, TenantId, BillingCycleCount, newAmount));

            return SubscriptionRenewalResult.CreateSuccess(Id, BillingCycleCount, newAmount);
        }
        catch (Exception ex)
        {
            return SubscriptionRenewalResult.Failed(Id, ex.Message);
        }
    }

    /// <summary>
    ///     Sets external IDs for payment provider integration
    /// </summary>
    public void SetExternalIds(string? subscriptionId, string? customerId)
    {
        ExternalId = subscriptionId;
        ExternalCustomerId = customerId;
    }

    /// <summary>
    ///     Sets auto-renewal preference
    /// </summary>
    public void SetAutoRenew(bool autoRenew)
    {
        if (Status == SubscriptionStatus.Cancelled)
            throw new InvalidOperationException("Cannot change auto-renewal for cancelled subscriptions");

        AutoRenew = autoRenew;
    }

    /// <summary>
    ///     Calculates billing dates based on start date and cycle
    /// </summary>
    private static (DateTime periodStart, DateTime periodEnd, DateTime nextBilling) CalculateBillingDates(
        DateTime startDate,
        BillingCycle cycle)
    {
        DateTime periodStart = startDate;
        (DateTime periodEnd, DateTime nextBilling) = cycle switch
        {
            BillingCycle.Monthly => (startDate.AddMonths(1).AddDays(-1), startDate.AddMonths(1)),
            BillingCycle.Quarterly => (startDate.AddMonths(3).AddDays(-1), startDate.AddMonths(3)),
            BillingCycle.SemiAnnually => (startDate.AddMonths(6).AddDays(-1), startDate.AddMonths(6)),
            BillingCycle.Annually => (startDate.AddYears(1).AddDays(-1), startDate.AddYears(1)),
            BillingCycle.Biannually => (startDate.AddYears(2).AddDays(-1), startDate.AddYears(2)),
            _ => throw new ArgumentOutOfRangeException(nameof(cycle))
        };

        return (periodStart, periodEnd, nextBilling);
    }
}


using GameGuild.SharedKernel;
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Unified service for subscription lifecycle, billing, and query operations.
///     Implements Single Responsibility: orchestrates repository and entity behavior.
/// </summary>
/// <remarks>
///     Architecture: This service follows the "thin service" pattern:
///     - Entity contains all business logic (state machine, validation)
///     - Repository handles persistence
///     - Service orchestrates load → mutate → save workflow
/// </remarks>
public class SubscriptionService : 
    ISubscriptionLifecycleService, 
    ISubscriptionBillingService, 
    ISubscriptionQueryService, 
    ISubscriptionExternalIdService
{
    private readonly ISubscriptionRepository _repository;
    private readonly ISubscriptionPlanService _planService;

    public SubscriptionService(ISubscriptionRepository repository, ISubscriptionPlanService planService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _planService = planService ?? throw new ArgumentNullException(nameof(planService));
    }

    #region Private Helpers (DRY principle)

    /// <summary>
    ///     Loads subscription or throws SubscriptionNotFoundException (fail-closed).
    /// </summary>
    private async Task<Subscription> GetRequiredAsync(Guid subscriptionId, CancellationToken ct)
    {
        var subscription = await _repository.GetByIdAsync(subscriptionId, ct).ConfigureAwait(false);
        return subscription ?? throw new SubscriptionNotFoundException(subscriptionId);
    }

    /// <summary>
    ///     Loads plan or throws InvalidOperationException (fail-closed).
    /// </summary>
    private async Task<SubscriptionPlan> GetRequiredPlanAsync(Guid planId, CancellationToken ct)
    {
        var plan = await _planService.GetByIdAsync(planId, ct).ConfigureAwait(false);
        return plan ?? throw new InvalidOperationException($"Subscription plan {planId} not found");
    }

    /// <summary>
    ///     Generates idempotency key for billing operations.
    /// </summary>
    private static string GenerateIdempotencyKey(Guid subscriptionId, int billingCycle, DateTime periodStart)
        => $"{subscriptionId}:{billingCycle}:{periodStart:yyyyMMdd}";

    /// <summary>
    ///     Calculates price for billing cycle from plan pricing.
    /// </summary>
    private static Money GetPriceForCycle(SubscriptionPlan plan, BillingCycle cycle)
    {
        var monthlyPrice = plan.GetMonthlyPrice();
        return cycle switch
        {
            BillingCycle.Monthly => monthlyPrice,
            BillingCycle.Annually => plan.GetAnnualPrice() ?? monthlyPrice * 12,
            BillingCycle.Quarterly => monthlyPrice * 3,
            BillingCycle.SemiAnnually => monthlyPrice * 6,
            BillingCycle.Biannually => monthlyPrice * 24,
            _ => monthlyPrice
        };
    }

    #endregion

    #region ISubscriptionLifecycleService

    public async Task<Subscription> CreateAsync(
        Guid tenantId,
        Guid planId,
        Guid createdByUserId,
        BillingCycle billingCycle,
        Money amount,
        DateTime? startDate = null,
        int? trialDays = null,
        CancellationToken cancellationToken = default)
    {
        // Validate plan exists
        await GetRequiredPlanAsync(planId, cancellationToken).ConfigureAwait(false);

        var effectiveStartDate = startDate ?? DateTime.UtcNow;
        DateTime? trialEndDate = trialDays.HasValue ? effectiveStartDate.AddDays(trialDays.Value) : null;

        var subscription = new Subscription(
            tenantId,
            planId,
            createdByUserId,
            billingCycle,
            amount,
            effectiveStartDate,
            trialEndDate
        );

        return await _repository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> ActivateAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.Activate();
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> StartTrialAsync(Guid subscriptionId, int trialDays, CancellationToken cancellationToken = default)
    {
        if (trialDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(trialDays), "Trial days must be positive");

        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.StartTrial(DateTime.UtcNow.AddDays(trialDays));
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> EndTrialAsync(Guid subscriptionId, bool convertToPaid, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.EndTrial(convertToPaid);
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> CancelAsync(Guid subscriptionId, CancellationReason reason, string? note = null, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.Cancel(reason, note, effectiveDate);
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> SuspendAsync(Guid subscriptionId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.Suspend(reason);
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> ReactivateAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.Reactivate();
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionUpgradeResult> UpgradePlanAsync(Guid subscriptionId, Guid newPlanId, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        var newPlan = await GetRequiredPlanAsync(newPlanId, cancellationToken).ConfigureAwait(false);
        var oldPlan = await GetRequiredPlanAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);

        // Validate upgrade (new plan should be higher tier)
        if (newPlan.GetMonthlyPrice().Amount <= oldPlan.GetMonthlyPrice().Amount)
            return SubscriptionUpgradeResult.Failed("New plan is not an upgrade. Use DowngradePlanAsync instead.");

        var newAmount = GetPriceForCycle(newPlan, subscription.BillingCycle);
        var proration = subscription.ChangePlan(newPlanId, newAmount, effectiveDate);
        await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

        // Convert decimal proration to Money
        var currency = subscription.Amount.Currency;
        var proratedAmount = new Money(proration.NetAdjustment, currency);
        var creditApplied = new Money(proration.CreditForUnused, currency);

        return SubscriptionUpgradeResult.CreateSuccess(subscription, proratedAmount, creditApplied);
    }

    public async Task<SubscriptionDowngradeResult> DowngradePlanAsync(Guid subscriptionId, Guid newPlanId, DateTime? effectiveDate = null, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        var newPlan = await GetRequiredPlanAsync(newPlanId, cancellationToken).ConfigureAwait(false);

        var newAmount = GetPriceForCycle(newPlan, subscription.BillingCycle);
        var proration = subscription.ChangePlan(newPlanId, newAmount, effectiveDate);

        // Downgrade takes effect at end of current period
        var actualEffectiveDate = effectiveDate ?? subscription.CurrentPeriodEnd;
        await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

        // Convert decimal proration to Money
        var currency = subscription.Amount.Currency;
        var creditIssued = new Money(proration.CreditForUnused, currency);

        return SubscriptionDowngradeResult.CreateSuccess(subscription, actualEffectiveDate, creditIssued);
    }

    public async Task<Subscription> ChangeBillingCycleAsync(Guid subscriptionId, BillingCycle newBillingCycle, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        var plan = await GetRequiredPlanAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);

        var newAmount = GetPriceForCycle(plan, newBillingCycle);
        subscription.ChangeBillingCycle(newBillingCycle, newAmount);

        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> SetAutoRenewAsync(Guid subscriptionId, bool autoRenew, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.SetAutoRenew(autoRenew);
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> UpdateMetadataAsync(Guid subscriptionId, string metadata, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.UpdateMetadata(metadata);
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region ISubscriptionBillingService

    public async Task<SubscriptionRenewalResult> ProcessRenewalAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        
        // Generate idempotency key to prevent duplicate charges
        var idempotencyKey = GenerateIdempotencyKey(
            subscriptionId, 
            subscription.BillingCycleCount + 1, 
            subscription.CurrentPeriodEnd
        );

        // Get current plan pricing (respects locked price version if set)
        var plan = await GetRequiredPlanAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        var renewalAmount = GetPriceForCycle(plan, subscription.BillingCycle);

        var result = subscription.ProcessRenewal(renewalAmount, idempotencyKey);
        
        if (result.Success)
        {
            await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    public async Task<Subscription> RecordPaymentAsync(Guid subscriptionId, decimal amount, string currency, DateTime paymentDate, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        
        // Generate payment idempotency key from payment details
        var idempotencyKey = $"payment:{subscriptionId}:{paymentDate:yyyyMMddHHmmss}:{amount}";
        
        var result = subscription.RecordPayment(amount, currency, paymentDate, idempotencyKey);
        
        if (!result.IsSuccess && !result.IsAlreadyProcessed)
        {
            throw new InvalidOperationException($"Failed to record payment: {result.Message}");
        }

        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription> RecordPaymentFailureAsync(Guid subscriptionId, string reason, DateTime failureDate, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.RecordPaymentFailure(reason, failureDate);
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BulkRenewalResult> ProcessBulkRenewalsAsync(IEnumerable<Guid> subscriptionIds, CancellationToken cancellationToken = default)
    {
        var attempts = new List<RenewalAttempt>();
        var totalRevenue = Money.Zero();
        var successCount = 0;
        var failCount = 0;

        foreach (var subscriptionId in subscriptionIds)
        {
            try
            {
                var result = await ProcessRenewalAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
                
                if (result.Success)
                {
                    successCount++;
                    if (result.ChargedAmount != null)
                    {
                        totalRevenue += result.ChargedAmount;
                    }
                    attempts.Add(new ConcreteRenewalAttempt 
                    { 
                        SubscriptionId = subscriptionId, 
                        Success = true, 
                        Amount = result.ChargedAmount 
                    });
                }
                else
                {
                    failCount++;
                    attempts.Add(new ConcreteRenewalAttempt 
                    { 
                        SubscriptionId = subscriptionId, 
                        Success = false, 
                        Amount = Money.Zero(), 
                        ErrorMessage = result.FailureReason 
                    });
                }
            }
            catch (Exception ex)
            {
                failCount++;
                attempts.Add(new ConcreteRenewalAttempt 
                { 
                    SubscriptionId = subscriptionId, 
                    Success = false, 
                    Amount = Money.Zero(), 
                    ErrorMessage = ex.Message 
                });
            }
        }

        return new ConcreteBulkRenewalResult
        {
            TotalProcessed = successCount + failCount,
            SuccessfulRenewals = successCount,
            FailedRenewals = failCount,
            TotalRevenue = totalRevenue,
            RenewalAttempts = attempts,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task SendRenewalRemindersAsync(int daysBeforeRenewal, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _repository.GetDueForRenewalAsync(daysBeforeRenewal, cancellationToken).ConfigureAwait(false);
        
        // NOTE: Notifications will be implemented when GameGuild.Notifications module is available.
        // This method currently retrieves subscriptions due for renewal but notification dispatch is pending.
        foreach (var subscription in subscriptions)
        {
            // Future: await _notificationService.SendRenewalReminderAsync(subscription, cancellationToken);
            _ = subscription; // Suppress unused variable warning until notification service is available
        }
    }

    public async Task SendTrialExpirationRemindersAsync(int daysBeforeExpiration, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _repository.GetTrialsExpiringSoonAsync(daysBeforeExpiration, cancellationToken).ConfigureAwait(false);
        
        // NOTE: Notifications will be implemented when GameGuild.Notifications module is available.
        foreach (var subscription in subscriptions)
        {
            // Future: await _notificationService.SendTrialExpirationReminderAsync(subscription, cancellationToken);
            _ = subscription;
        }
    }

    #endregion

    #region ISubscriptionQueryService

    public async Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Subscription?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByExternalIdAsync(externalId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsSubscriptionActiveAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetActiveTenantSubscriptionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return subscription != null && subscription.Status == SubscriptionStatus.Active;
    }

    public async Task<Subscription?> GetActiveTenantSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetActiveTenantSubscriptionAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetTenantSubscriptionsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetTenantSubscriptionHistoryAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Return all subscriptions for tenant, sorted by creation date (includes cancelled/expired)
        return await _repository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetExpiringSoonAsync(int days, CancellationToken cancellationToken = default)
    {
        return await _repository.GetExpiringSoonAsync(days, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetDueForRenewalAsync(int days, CancellationToken cancellationToken = default)
    {
        return await _repository.GetDueForRenewalAsync(days, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Subscription>> GetTrialsExpiringSoonAsync(int days, CancellationToken cancellationToken = default)
    {
        return await _repository.GetTrialsExpiringSoonAsync(days, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SubscriptionLimitValidationResult> ValidateSubscriptionLimitsAsync(Guid tenantId, int userCount, long storageMb, long apiCallsPerMonth, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetActiveTenantSubscriptionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        
        if (subscription == null)
            return SubscriptionLimitValidationResult.Invalid(
                new List<LimitCheckResult> { new ConcreteLimitCheckResult { LimitName = "Subscription", CurrentUsage = 0, MaxAllowed = 1, Passed = false } },
                "Please subscribe to a plan"
            );

        var plan = await GetRequiredPlanAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);
        var failedChecks = new List<LimitCheckResult>();

        // Check user limit
        if (plan.MaxUsers.HasValue && userCount > plan.MaxUsers.Value)
        {
            failedChecks.Add(new ConcreteLimitCheckResult 
            { 
                LimitName = "Users", 
                CurrentUsage = userCount, 
                MaxAllowed = plan.MaxUsers.Value, 
                Passed = false 
            });
        }

        // Check storage limit
        if (plan.MaxStorageMb.HasValue && storageMb > plan.MaxStorageMb.Value)
        {
            failedChecks.Add(new ConcreteLimitCheckResult 
            { 
                LimitName = "Storage", 
                CurrentUsage = storageMb, 
                MaxAllowed = plan.MaxStorageMb.Value, 
                Passed = false 
            });
        }

        // Check API calls limit
        if (plan.MaxApiCallsPerMonth.HasValue && apiCallsPerMonth > plan.MaxApiCallsPerMonth.Value)
        {
            failedChecks.Add(new ConcreteLimitCheckResult 
            { 
                LimitName = "API Calls", 
                CurrentUsage = apiCallsPerMonth, 
                MaxAllowed = plan.MaxApiCallsPerMonth.Value, 
                Passed = false 
            });
        }

        return failedChecks.Count > 0
            ? SubscriptionLimitValidationResult.Invalid(failedChecks, "Consider upgrading your plan")
            : SubscriptionLimitValidationResult.Valid();
    }

    public async Task<SubscriptionUsageStatistics> GetUsageStatisticsAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        var plan = await GetRequiredPlanAsync(subscription.PlanId, cancellationToken).ConfigureAwait(false);

        // Return basic statistics from subscription and plan data
        // Full usage tracking would require integration with usage tracking service
        return new ConcreteSubscriptionUsageStatistics
        {
            SubscriptionId = subscriptionId,
            UserCount = 0, // Would come from usage tracking service
            StorageUsedMb = 0,
            ApiCallsThisMonth = 0,
            PlanLimits = new SubscriptionPlanLimits
            {
                MaxUsers = plan.MaxUsers ?? int.MaxValue,
                MaxStorageMb = plan.MaxStorageMb ?? long.MaxValue,
                MaxApiCallsPerMonth = plan.MaxApiCallsPerMonth ?? long.MaxValue,
                UnlimitedUsers = !plan.MaxUsers.HasValue,
                UnlimitedStorage = !plan.MaxStorageMb.HasValue,
                UnlimitedApiCalls = !plan.MaxApiCallsPerMonth.HasValue
            },
            PeriodStart = subscription.CurrentPeriodStart,
            PeriodEnd = subscription.CurrentPeriodEnd,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<RevenueAnalytics> GetRevenueAnalyticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _repository.GetByDateRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);
        var subscriptionList = subscriptions.ToList();

        var totalRevenue = subscriptionList
            .Where(s => s.Status == SubscriptionStatus.Active || s.LastPaymentAt.HasValue)
            .Sum(s => s.Amount.Amount);

        var cycleBreakdown = subscriptionList
            .GroupBy(s => s.BillingCycle)
            .ToDictionary(g => g.Key, g => new Money(g.Sum(s => s.Amount.Amount), "USD"));

        var planBreakdown = subscriptionList
            .GroupBy(s => s.PlanId)
            .ToDictionary(g => g.Key, g => new Money(g.Sum(s => s.Amount.Amount), "USD"));

        var newSubCount = subscriptionList.Count(s => s.CreatedAt >= startDate);
        var averageAmount = subscriptionList.FirstOrDefault()?.Amount.Amount ?? 0m;

        return new ConcreteRevenueAnalytics
        {
            TotalRevenue = new Money(totalRevenue, "USD"),
            NewSubscriptionRevenue = new Money(newSubCount * averageAmount, "USD"),
            RenewalRevenue = Money.Zero(),
            UpgradeRevenue = Money.Zero(),
            AddOnRevenue = Money.Zero(),
            RefundAmount = Money.Zero(),
            PeriodStart = startDate,
            PeriodEnd = endDate,
            BillingCycleBreakdown = cycleBreakdown,
            PlanBreakdown = planBreakdown,
            TransactionCount = subscriptionList.Count
        };
    }

    public async Task<SubscriptionAnalytics> GetSubscriptionAnalyticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var statusCounts = await _repository.GetCountByStatusAsync(cancellationToken).ConfigureAwait(false);
        var subscriptions = await _repository.GetByDateRangeAsync(startDate, endDate, cancellationToken).ConfigureAwait(false);
        var subscriptionList = subscriptions.ToList();

        var total = statusCounts.Values.Sum();
        var active = statusCounts.GetValueOrDefault(SubscriptionStatus.Active);
        var trialing = statusCounts.GetValueOrDefault(SubscriptionStatus.Trialing);
        var cancelled = statusCounts.GetValueOrDefault(SubscriptionStatus.Cancelled);
        var suspended = statusCounts.GetValueOrDefault(SubscriptionStatus.Suspended);

        var newInPeriod = subscriptionList.Count(s => s.CreatedAt >= startDate);
        var cancelledInPeriod = subscriptionList.Count(s => s.CancelledAt.HasValue && s.CancelledAt >= startDate);

        // Calculate churn rate safely
        var churnRate = active > 0 ? (decimal)cancelledInPeriod / active * 100 : 0;
        var growthRate = active > 0 ? (decimal)(newInPeriod - cancelledInPeriod) / active * 100 : 0;

        var activeSubscriptions = subscriptionList.Where(s => s.Status == SubscriptionStatus.Active).ToList();
        var monthlyRevenue = activeSubscriptions.Sum(s => s.Amount.Amount);

        return new ConcreteSubscriptionAnalytics
        {
            TotalSubscriptions = total,
            ActiveSubscriptions = active,
            TrialingSubscriptions = trialing,
            CancelledSubscriptions = cancelled,
            SuspendedSubscriptions = suspended,
            NewSubscriptions = newInPeriod,
            CancellationsInPeriod = cancelledInPeriod,
            ChurnRate = churnRate,
            GrowthRate = growthRate,
            PeriodStart = startDate,
            PeriodEnd = endDate,
            MonthlyRecurringRevenue = new Money(monthlyRevenue, "USD"),
            AnnualRecurringRevenue = new Money(monthlyRevenue * 12, "USD")
        };
    }

    #endregion

    #region ISubscriptionExternalIdService

    public async Task<Subscription> SetExternalIdsAsync(Guid subscriptionId, string? externalSubscriptionId, string? externalCustomerId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetRequiredAsync(subscriptionId, cancellationToken).ConfigureAwait(false);
        subscription.SetExternalIds(externalSubscriptionId, externalCustomerId);
        return await _repository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
    }

    Task<Subscription?> ISubscriptionExternalIdService.GetByExternalIdAsync(string externalId, CancellationToken cancellationToken)
    {
        return _repository.GetByExternalIdAsync(externalId, cancellationToken);
    }

    #endregion
}

#region Concrete implementations for abstract result types (required for instantiation)

/// <summary>
///     Concrete implementation of BulkRenewalResult for instantiation.
/// </summary>
internal sealed class ConcreteBulkRenewalResult : BulkRenewalResult;

/// <summary>
///     Concrete implementation of SubscriptionUsageStatistics for instantiation.
/// </summary>
internal sealed class ConcreteSubscriptionUsageStatistics : SubscriptionUsageStatistics;

/// <summary>
///     Concrete implementation of RevenueAnalytics for instantiation.
/// </summary>
internal sealed class ConcreteRevenueAnalytics : RevenueAnalytics;

/// <summary>
///     Concrete implementation of SubscriptionAnalytics for instantiation.
/// </summary>
internal sealed class ConcreteSubscriptionAnalytics : SubscriptionAnalytics;

/// <summary>
///     Concrete implementation of RenewalAttempt for instantiation.
/// </summary>
internal sealed class ConcreteRenewalAttempt : RenewalAttempt;

/// <summary>
///     Concrete implementation of LimitCheckResult for instantiation.
/// </summary>
internal sealed class ConcreteLimitCheckResult : LimitCheckResult;

#endregion

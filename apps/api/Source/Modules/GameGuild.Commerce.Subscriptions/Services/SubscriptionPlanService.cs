using Microsoft.Extensions.Caching.Memory;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Service implementation for subscription plan business operations with caching
/// </summary>
public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _planRepository;
    private readonly IMemoryCache _cache;

    // Cache keys
    private const string ActivePlansCacheKey = "SubscriptionPlans:Active";
    private const string PlanByIdCacheKeyPrefix = "SubscriptionPlans:ById:";

    // Cache durations
    private static readonly TimeSpan ActivePlansCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PlanByIdCacheDuration = TimeSpan.FromMinutes(10);

    public SubscriptionPlanService(ISubscriptionPlanRepository planRepository, IMemoryCache cache)
    {
        _planRepository = planRepository;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> CreateAsync(
        string name,
        string slug,
        long monthlyPriceInCents,
        string currency = "USD",
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        // Validate uniqueness
        if (!await _planRepository.IsNameUniqueAsync(name, cancellationToken: cancellationToken))
            throw new InvalidOperationException($"A subscription plan with name '{name}' already exists.");

        if (!await _planRepository.IsSlugUniqueAsync(slug, cancellationToken: cancellationToken))
            throw new InvalidOperationException($"A subscription plan with slug '{slug}' already exists.");

        var plan = new SubscriptionPlan(name, slug, monthlyPriceInCents, currency, description);
        var result = await _planRepository.AddAsync(plan, cancellationToken).ConfigureAwait(false);

        // Invalidate active plans cache since a new plan was added
        _cache.Remove(ActivePlansCacheKey);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> UpdateAsync(
        Guid planId,
        string name,
        string? description = null,
        int? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);

        // Validate name uniqueness if changed
        if (!string.Equals(plan.Name, name, StringComparison.OrdinalIgnoreCase) &&
            !await _planRepository.IsNameUniqueAsync(name, planId, cancellationToken))
            throw new InvalidOperationException($"A subscription plan with name '{name}' already exists.");

        plan.UpdateDetails(name, description, sortOrder);
        var result = await _planRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> UpdatePricingAsync(
        Guid planId,
        long monthlyPriceInCents,
        long? annualPriceInCents = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);
        plan.UpdatePricing(monthlyPriceInCents, annualPriceInCents);
        var result = await _planRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> UpdateLimitsAsync(
        Guid planId,
        int? maxUsers = null,
        long? maxStorageMb = null,
        long? maxApiCallsPerMonth = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);
        plan.UpdateLimits(maxUsers, maxStorageMb, maxApiCallsPerMonth);
        var result = await _planRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> UpdateFeaturesAsync(
        Guid planId,
        bool? hasPrioritySupport = null,
        bool? hasAdvancedAnalytics = null,
        bool? hasCustomBranding = null,
        string? features = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);
        plan.UpdateFeatures(hasPrioritySupport, hasAdvancedAnalytics, hasCustomBranding, features);
        var result = await _planRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> ActivateAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);
        plan.Activate();
        var result = await _planRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> DeactivateAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);
        plan.Deactivate();
        var result = await _planRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> SetFeaturedAsync(Guid planId, bool featured = true, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);
        plan.SetFeatured(featured);
        var result = await _planRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> SetExternalIdAsync(Guid planId, string externalId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);
        plan.SetExternalId(externalId);
        var result = await _planRepository.UpdateAsync(plan, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);

        return result;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{PlanByIdCacheKeyPrefix}{planId}";

        if (_cache.TryGetValue(cacheKey, out SubscriptionPlan? cachedPlan))
            return cachedPlan;

        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);

        if (plan is not null)
        {
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(PlanByIdCacheDuration)
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));

            _cache.Set(cacheKey, plan, cacheOptions);
        }

        return plan;
    }

    /// <inheritdoc />
    public Task<SubscriptionPlan?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _planRepository.GetBySlugAsync(slug, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionPlan>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(ActivePlansCacheKey, out IEnumerable<SubscriptionPlan>? cachedPlans) && cachedPlans is not null)
            return cachedPlans;

        var plans = await _planRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        var plansList = plans.ToList();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(ActivePlansCacheDuration)
            .SetSlidingExpiration(TimeSpan.FromMinutes(1));

        _cache.Set(ActivePlansCacheKey, plansList, cacheOptions);

        return plansList;
    }

    /// <inheritdoc />
    public Task<IEnumerable<SubscriptionPlan>> GetFeaturedAsync(CancellationToken cancellationToken = default)
    {
        return _planRepository.GetFeaturedAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SubscriptionPlan>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return _planRepository.SearchByNameAsync(searchTerm, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SubscriptionPlan>> GetByPriceRangeAsync(
        long minPriceInCents,
        long maxPriceInCents,
        CancellationToken cancellationToken = default)
    {
        return _planRepository.GetByPriceRangeAsync(minPriceInCents, maxPriceInCents, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PlanValidationResult> ValidatePlanLimitsAsync(
        Guid planId,
        int userCount,
        long storageMb,
        long apiCallsPerMonth,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);

        var errors = new List<string>();

        if (!plan.AllowsUserCount(userCount))
            errors.Add($"Plan allows max {plan.MaxUsers} users, but {userCount} requested.");

        if (!plan.AllowsStorage(storageMb))
            errors.Add($"Plan allows max {plan.MaxStorageMb}MB storage, but {storageMb}MB requested.");

        if (!plan.AllowsApiCalls(apiCallsPerMonth))
            errors.Add($"Plan allows max {plan.MaxApiCallsPerMonth} API calls/month, but {apiCallsPerMonth} requested.");

        if (errors.Count == 0)
            return PlanValidationResult.Success();

        // Find suitable upgrade plans
        var activePlans = await _planRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        var suggestedUpgrades = activePlans
            .Where(p => p.Id != planId &&
                        p.AllowsUserCount(userCount) &&
                        p.AllowsStorage(storageMb) &&
                        p.AllowsApiCalls(apiCallsPerMonth) &&
                        p.MonthlyPriceInCents > plan.MonthlyPriceInCents)
            .OrderBy(p => p.MonthlyPriceInCents)
            .Take(3)
            .Select(p => p.Id)
            .ToList();

        return PlanValidationResult.FailureWithSuggestions(errors, suggestedUpgrades);
    }

    /// <inheritdoc />
    public async Task<PlanUsageStatistics> GetUsageStatisticsAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken).ConfigureAwait(false);
        var activeSubscriptionCount = await _planRepository.GetActiveSubscriptionCountAsync(planId, cancellationToken).ConfigureAwait(false);

        // Return basic statistics - more detailed analytics would require additional repository methods
        return new DefaultPlanUsageStatistics
        {
            PlanId = planId,
            ActiveSubscriptions = activeSubscriptionCount,
            CancelledSubscriptions = 0, // Would need additional query
            AverageMonthlyRevenue = activeSubscriptionCount * (plan.MonthlyPriceInCents / 100m),
            TotalRevenue = 0, // Would need payment history query
            AverageSubscriptionDurationDays = 0, // Would need subscription history query
            TrialConversionRate = null,
            ChurnRate = 0
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubscriptionPlan>> SuggestUpgradesAsync(
        Guid currentPlanId,
        int currentUserCount,
        long currentStorageMb,
        long currentApiCallsPerMonth,
        CancellationToken cancellationToken = default)
    {
        var currentPlan = await GetPlanOrThrowAsync(currentPlanId, cancellationToken).ConfigureAwait(false);
        var activePlans = await _planRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);

        // Find plans that accommodate current usage and are more expensive (upgrades)
        return activePlans
            .Where(p => p.Id != currentPlanId &&
                        p.AllowsUserCount(currentUserCount) &&
                        p.AllowsStorage(currentStorageMb) &&
                        p.AllowsApiCalls(currentApiCallsPerMonth) &&
                        p.MonthlyPriceInCents > currentPlan.MonthlyPriceInCents)
            .OrderBy(p => p.MonthlyPriceInCents)
            .Take(5);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var activeSubscriptionCount = await _planRepository.GetActiveSubscriptionCountAsync(planId, cancellationToken).ConfigureAwait(false);
        if (activeSubscriptionCount > 0)
            throw new InvalidOperationException($"Cannot delete plan with {activeSubscriptionCount} active subscriptions. Deactivate the plan instead.");

        await _planRepository.DeleteAsync(planId, cancellationToken).ConfigureAwait(false);

        InvalidatePlanCache(planId);
    }

    private async Task<SubscriptionPlan> GetPlanOrThrowAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken).ConfigureAwait(false);
        if (plan == null)
            throw new InvalidOperationException($"Subscription plan with ID '{planId}' not found.");
        return plan;
    }

    /// <summary>
    ///     Default implementation of PlanUsageStatistics for service return
    /// </summary>
    private class DefaultPlanUsageStatistics : PlanUsageStatistics
    {
        public new DateTime CalculatedAt { get; set; } = SystemClock.UtcNow;
    }

    /// <summary>
    ///     Invalidates cache entries for a specific plan
    /// </summary>
    private void InvalidatePlanCache(Guid planId)
    {
        _cache.Remove($"{PlanByIdCacheKeyPrefix}{planId}");
        _cache.Remove(ActivePlansCacheKey);
    }
}

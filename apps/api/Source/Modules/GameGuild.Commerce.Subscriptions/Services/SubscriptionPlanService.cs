namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Service implementation for subscription plan business operations
/// </summary>
public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _planRepository;

    public SubscriptionPlanService(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
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
        return await _planRepository.AddAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> UpdateAsync(
        Guid planId,
        string name,
        string? description = null,
        int? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);

        // Validate name uniqueness if changed
        if (!string.Equals(plan.Name, name, StringComparison.OrdinalIgnoreCase) &&
            !await _planRepository.IsNameUniqueAsync(name, planId, cancellationToken))
            throw new InvalidOperationException($"A subscription plan with name '{name}' already exists.");

        plan.UpdateDetails(name, description, sortOrder);
        return await _planRepository.UpdateAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> UpdatePricingAsync(
        Guid planId,
        long monthlyPriceInCents,
        long? annualPriceInCents = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);
        plan.UpdatePricing(monthlyPriceInCents, annualPriceInCents);
        return await _planRepository.UpdateAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> UpdateLimitsAsync(
        Guid planId,
        int? maxUsers = null,
        long? maxStorageMb = null,
        long? maxApiCallsPerMonth = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);
        plan.UpdateLimits(maxUsers, maxStorageMb, maxApiCallsPerMonth);
        return await _planRepository.UpdateAsync(plan, cancellationToken);
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
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);
        plan.UpdateFeatures(hasPrioritySupport, hasAdvancedAnalytics, hasCustomBranding, features);
        return await _planRepository.UpdateAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> ActivateAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);
        plan.Activate();
        return await _planRepository.UpdateAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> DeactivateAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);
        plan.Deactivate();
        return await _planRepository.UpdateAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> SetFeaturedAsync(Guid planId, bool featured = true, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);
        plan.SetFeatured(featured);
        return await _planRepository.UpdateAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> SetExternalIdAsync(Guid planId, string externalId, CancellationToken cancellationToken = default)
    {
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);
        plan.SetExternalId(externalId);
        return await _planRepository.UpdateAsync(plan, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SubscriptionPlan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        return _planRepository.GetByIdAsync(planId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<SubscriptionPlan?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return _planRepository.GetBySlugAsync(slug, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<SubscriptionPlan>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return _planRepository.GetActiveAsync(cancellationToken);
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
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);

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
        var activePlans = await _planRepository.GetActiveAsync(cancellationToken);
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
        var plan = await GetPlanOrThrowAsync(planId, cancellationToken);
        var activeSubscriptionCount = await _planRepository.GetActiveSubscriptionCountAsync(planId, cancellationToken);

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
        var currentPlan = await GetPlanOrThrowAsync(currentPlanId, cancellationToken);
        var activePlans = await _planRepository.GetActiveAsync(cancellationToken);

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
        var activeSubscriptionCount = await _planRepository.GetActiveSubscriptionCountAsync(planId, cancellationToken);
        if (activeSubscriptionCount > 0)
            throw new InvalidOperationException($"Cannot delete plan with {activeSubscriptionCount} active subscriptions. Deactivate the plan instead.");

        await _planRepository.DeleteAsync(planId, cancellationToken);
    }

    private async Task<SubscriptionPlan> GetPlanOrThrowAsync(Guid planId, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(planId, cancellationToken);
        if (plan == null)
            throw new InvalidOperationException($"Subscription plan with ID '{planId}' not found.");
        return plan;
    }

    /// <summary>
    ///     Default implementation of PlanUsageStatistics for service return
    /// </summary>
    private class DefaultPlanUsageStatistics : PlanUsageStatistics
    {
        public new DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}

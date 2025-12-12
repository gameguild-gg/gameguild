using GameGuild.Subscriptions.SubscriptionPlans.Entities;
using GameGuild.Subscriptions.SubscriptionPlans.Models;

namespace GameGuild.Subscriptions.SubscriptionPlans.Abstractions;

/// <summary>
///     Service interface for subscription plan business operations
/// </summary>
public interface ISubscriptionPlanService
{
    /// <summary>
    ///     Creates a new subscription plan
    /// </summary>
    Task<SubscriptionPlan> CreateAsync(string name, string slug, long monthlyPriceInCents, string currency = "USD", string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing subscription plan
    /// </summary>
    Task<SubscriptionPlan> UpdateAsync(Guid planId, string name, string? description = null, int? sortOrder = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates plan pricing
    /// </summary>
    Task<SubscriptionPlan> UpdatePricingAsync(Guid planId, long monthlyPriceInCents, long? annualPriceInCents = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates plan limits
    /// </summary>
    Task<SubscriptionPlan> UpdateLimitsAsync(Guid planId, int? maxUsers = null, long? maxStorageMb = null, long? maxApiCallsPerMonth = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates plan features
    /// </summary>
    Task<SubscriptionPlan> UpdateFeaturesAsync(
        Guid planId,
        bool? hasPrioritySupport = null,
        bool? hasAdvancedAnalytics = null,
        bool? hasCustomBranding = null,
        string? features = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    ///     Activates a subscription plan
    /// </summary>
    Task<SubscriptionPlan> ActivateAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deactivates a subscription plan
    /// </summary>
    Task<SubscriptionPlan> DeactivateAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets a plan as featured
    /// </summary>
    Task<SubscriptionPlan> SetFeaturedAsync(Guid planId, bool featured = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sets external ID for payment provider integration
    /// </summary>
    Task<SubscriptionPlan> SetExternalIdAsync(Guid planId, string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a subscription plan by ID
    /// </summary>
    Task<SubscriptionPlan?> GetByIdAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a subscription plan by slug
    /// </summary>
    Task<SubscriptionPlan?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all active subscription plans
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets featured subscription plans
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> GetFeaturedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Searches subscription plans
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets subscription plans within a price range
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> GetByPriceRangeAsync(long minPriceInCents, long maxPriceInCents, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Validates if a plan can support the specified limits
    /// </summary>
    Task<PlanValidationResult> ValidatePlanLimitsAsync(Guid planId, int userCount, long storageMb, long apiCallsPerMonth, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets plan usage statistics
    /// </summary>
    Task<PlanUsageStatistics> GetUsageStatisticsAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Suggests plan upgrades based on current usage
    /// </summary>
    Task<IEnumerable<SubscriptionPlan>> SuggestUpgradesAsync(Guid currentPlanId, int currentUserCount, long currentStorageMb, long currentApiCallsPerMonth, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a subscription plan (only if no active subscriptions)
    /// </summary>
    Task DeleteAsync(Guid planId, CancellationToken cancellationToken = default);
}

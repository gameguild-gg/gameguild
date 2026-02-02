using GameGuild.Models;

namespace GameGuild.Features;

/// <summary>
/// Service interface for subscription-aware feature access control.
/// Integrates subscription plans with feature flag evaluation.
/// </summary>
public interface ISubscriptionFeatureService
{
    /// <summary>
    /// Checks if a feature is available for a tenant based on their subscription plan
    /// </summary>
    Task<bool> IsFeatureAvailableForTenantAsync(Guid tenantId, string featureKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a feature is available for a user based on their tenant's subscription
    /// </summary>
    Task<bool> IsFeatureAvailableForUserAsync(Guid userId, Guid tenantId, string featureKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all features available for a tenant's subscription plan
    /// </summary>
    Task<IEnumerable<string>> GetAvailableFeaturesForTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets features that would be unlocked by upgrading to a specific plan
    /// </summary>
    Task<IEnumerable<string>> GetFeaturesUnlockedByPlanAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a tenant can use a specific feature, returning detailed result
    /// </summary>
    Task<Result<SubscriptionFeatureAccessResult>> ValidateFeatureAccessAsync(
        Guid tenantId, 
        string featureKey, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets feature entitlements comparison between current plan and target plan
    /// </summary>
    Task<FeatureEntitlementComparison> CompareFeatureEntitlementsAsync(
        Guid currentPlanId,
        Guid targetPlanId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a subscription-based feature access check
/// </summary>
public record SubscriptionFeatureAccessResult(
    bool IsAllowed,
    string FeatureKey,
    string? PlanName,
    string? Reason,
    string? UpgradeUrl);

/// <summary>
/// Comparison of feature entitlements between two plans
/// </summary>
public record FeatureEntitlementComparison(
    Guid CurrentPlanId,
    string CurrentPlanName,
    Guid TargetPlanId,
    string TargetPlanName,
    IEnumerable<string> SharedFeatures,
    IEnumerable<string> NewFeatures,
    IEnumerable<string> LostFeatures);

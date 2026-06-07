using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CommerceSubscription = GameGuild.Commerce.Subscriptions.Subscription;
using CommerceSubscriptionPlan = GameGuild.Commerce.Subscriptions.SubscriptionPlan;
using CommerceSubscriptionStatus = GameGuild.Commerce.Subscriptions.SubscriptionStatus;

namespace GameGuild.Features;

/// <summary>
/// Service implementation that integrates subscription plans with feature flag evaluation.
/// Provides subscription-aware feature access control.
/// </summary>
public class SubscriptionFeatureService : ISubscriptionFeatureService
{
    private readonly IApplicationDbContext _context;
    private readonly IFeatureFlagEvaluationService _featureFlagService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SubscriptionFeatureService> _logger;

    private const string TenantFeaturesCacheKeyPrefix = "TenantFeatures:";
    private const string PlanFeaturesCacheKeyPrefix = "PlanFeatures:";
    [ExcludeFromCodeCoverage]
    private static TimeSpan CacheDuration { get; } = TimeSpan.FromMinutes(5);

    public SubscriptionFeatureService(
        IApplicationDbContext context,
        IFeatureFlagEvaluationService featureFlagService,
        IMemoryCache cache,
        ILogger<SubscriptionFeatureService> logger)
    {
        _context = context;
        _featureFlagService = featureFlagService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsFeatureAvailableForTenantAsync(
        Guid tenantId, 
        string featureKey, 
        CancellationToken cancellationToken = default)
    {
        var result = await ValidateFeatureAccessAsync(tenantId, featureKey, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess && result.Value.IsAllowed;
    }

    public async Task<bool> IsFeatureAvailableForUserAsync(
        Guid userId, 
        Guid tenantId, 
        string featureKey, 
        CancellationToken cancellationToken = default)
    {
        // First check subscription-level access
        var subscriptionAccess = await IsFeatureAvailableForTenantAsync(tenantId, featureKey, cancellationToken).ConfigureAwait(false);
        if (!subscriptionAccess)
        {
            return false;
        }

        // Then check feature flag evaluation for user-specific targeting
        var context = new FeatureContext
        {
            UserId = userId,
            TenantId = tenantId,
            Environment = "production"
        };

        return await _featureFlagService.IsEnabledAsync(featureKey, context, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetAvailableFeaturesForTenantAsync(
        Guid tenantId, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{TenantFeaturesCacheKeyPrefix}{tenantId}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cachedFeatures) && cachedFeatures != null)
        {
            return cachedFeatures;
        }

        // Get tenant's active subscription with plan
        var subscription = await _context.Set<CommerceSubscription>()
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId && s.Status == CommerceSubscriptionStatus.Active)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (subscription?.Plan == null)
        {
            _logger.LogWarning("No active subscription found for tenant {TenantId}", tenantId);
            return Enumerable.Empty<string>();
        }

        var features = ParsePlanFeatures(subscription.Plan.Features);

        _cache.Set(cacheKey, features, CacheDuration);

        return features;
    }

    public async Task<IEnumerable<string>> GetFeaturesUnlockedByPlanAsync(
        Guid planId, 
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{PlanFeaturesCacheKeyPrefix}{planId}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cachedFeatures) && cachedFeatures != null)
        {
            return cachedFeatures;
        }

        var plan = await _context.Set<CommerceSubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken).ConfigureAwait(false);

        if (plan == null)
        {
            _logger.LogWarning("Subscription plan {PlanId} not found", planId);
            return Enumerable.Empty<string>();
        }

        var features = ParsePlanFeatures(plan.Features);

        _cache.Set(cacheKey, features, CacheDuration);

        return features;
    }

    public async Task<Result<SubscriptionFeatureAccessResult>> ValidateFeatureAccessAsync(
        Guid tenantId, 
        string featureKey, 
        CancellationToken cancellationToken = default)
    {
        // Get tenant's active subscription with plan
        var subscription = await _context.Set<CommerceSubscription>()
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId && s.Status == CommerceSubscriptionStatus.Active)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (subscription == null)
        {
            return Result.Success(new SubscriptionFeatureAccessResult(
                IsAllowed: false,
                FeatureKey: featureKey,
                PlanName: null,
                Reason: "No active subscription found",
                UpgradeUrl: "/pricing"));
        }

        if (subscription.Plan == null)
        {
            return Result.Success(new SubscriptionFeatureAccessResult(
                IsAllowed: false,
                FeatureKey: featureKey,
                PlanName: null,
                Reason: "Subscription plan not found",
                UpgradeUrl: "/pricing"));
        }

        var planFeatures = ParsePlanFeatures(subscription.Plan.Features);
        var hasFeature = planFeatures.Contains(featureKey, StringComparer.OrdinalIgnoreCase);

        // Check for wildcard access (e.g., "all" or "*" grants access to everything)
        var hasWildcard = planFeatures.Any(f => f == "*" || f.Equals("all", StringComparison.OrdinalIgnoreCase));

        if (hasFeature || hasWildcard)
        {
            // Also check if the feature flag itself is enabled
            var featureFlagContext = new FeatureContext
            {
                TenantId = tenantId,
                Environment = "production"
            };

            var isFeatureFlagEnabled = await _featureFlagService.IsEnabledAsync(featureKey, featureFlagContext, cancellationToken).ConfigureAwait(false);

            if (!isFeatureFlagEnabled)
            {
                return Result.Success(new SubscriptionFeatureAccessResult(
                    IsAllowed: false,
                    FeatureKey: featureKey,
                    PlanName: subscription.Plan.Name,
                    Reason: "Feature is currently disabled",
                    UpgradeUrl: null));
            }

            return Result.Success(new SubscriptionFeatureAccessResult(
                IsAllowed: true,
                FeatureKey: featureKey,
                PlanName: subscription.Plan.Name,
                Reason: null,
                UpgradeUrl: null));
        }

        // Find a plan that includes this feature for upgrade suggestion
        var upgradePlan = await _context.Set<CommerceSubscriptionPlan>()
            .Where(p => p.IsActive && p.Features != null && p.Features.Contains(featureKey))
            .OrderBy(p => p.MonthlyPriceInCents)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(new SubscriptionFeatureAccessResult(
            IsAllowed: false,
            FeatureKey: featureKey,
            PlanName: subscription.Plan.Name,
            Reason: $"Feature '{featureKey}' is not included in your {subscription.Plan.Name} plan",
            UpgradeUrl: upgradePlan != null ? $"/pricing?upgrade={upgradePlan.Slug}" : "/pricing"));
    }

    public async Task<FeatureEntitlementComparison> CompareFeatureEntitlementsAsync(
        Guid currentPlanId, 
        Guid targetPlanId, 
        CancellationToken cancellationToken = default)
    {
        var currentPlan = await _context.Set<CommerceSubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == currentPlanId, cancellationToken).ConfigureAwait(false);

        var targetPlan = await _context.Set<CommerceSubscriptionPlan>()
            .FirstOrDefaultAsync(p => p.Id == targetPlanId, cancellationToken).ConfigureAwait(false);

        var currentFeatures = ParsePlanFeatures(currentPlan?.Features).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var targetFeatures = ParsePlanFeatures(targetPlan?.Features).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new FeatureEntitlementComparison(
            CurrentPlanId: currentPlanId,
            CurrentPlanName: currentPlan?.Name ?? "Unknown",
            TargetPlanId: targetPlanId,
            TargetPlanName: targetPlan?.Name ?? "Unknown",
            SharedFeatures: currentFeatures.Intersect(targetFeatures),
            NewFeatures: targetFeatures.Except(currentFeatures),
            LostFeatures: currentFeatures.Except(targetFeatures));
    }

    private static IEnumerable<string> ParsePlanFeatures(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson))
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            // Try parsing as JSON array
            var features = JsonSerializer.Deserialize<List<string>>(featuresJson);
            return features ?? Enumerable.Empty<string>();
        }
        catch (JsonException)
        {
            // Fallback: treat as comma-separated string
            return featuresJson.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}

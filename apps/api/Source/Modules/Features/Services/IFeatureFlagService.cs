using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Services;

/// <summary>
/// Service interface for feature flag management and evaluation
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Evaluate a feature flag with context
    /// </summary>
    Task<FeatureEvaluationResult> EvaluateFeatureAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get boolean feature flag value
    /// </summary>
    Task<bool> GetBooleanAsync(string featureKey, bool defaultValue = false, FeatureContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get string feature flag value
    /// </summary>
    Task<string> GetStringAsync(string featureKey, string defaultValue = "", FeatureContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get integer feature flag value
    /// </summary>
    Task<int> GetIntAsync(string featureKey, int defaultValue = 0, FeatureContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get double feature flag value
    /// </summary>
    Task<double> GetDoubleAsync(string featureKey, double defaultValue = 0d, FeatureContext? context = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new feature flag
    /// </summary>
    Task<FeatureFlag> CreateFeatureFlagAsync(FeatureFlag featureFlag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing feature flag
    /// </summary>
    Task<FeatureFlag?> UpdateFeatureFlagAsync(Guid id, FeatureFlag featureFlag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a feature flag
    /// </summary>
    Task<bool> DeleteFeatureFlagAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get feature flag by ID
    /// </summary>
    Task<FeatureFlag?> GetFeatureFlagByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get feature flag by key
    /// </summary>
    Task<FeatureFlag?> GetFeatureFlagByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all feature flags
    /// </summary>
    Task<IEnumerable<FeatureFlag>> GetFeatureFlagsAsync(Guid? tenantId = null, string? environment = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get feature flag usage analytics
    /// </summary>
    Task<IEnumerable<FeatureFlagUsage>> GetUsageAnalyticsAsync(Guid featureFlagId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}

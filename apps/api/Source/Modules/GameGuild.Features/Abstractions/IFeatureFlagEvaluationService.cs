
namespace GameGuild.Features;

/// <summary>
///     Service interface for evaluating feature flags.
///     Follows Interface Segregation Principle (ISP) by focusing only on feature evaluation.
/// </summary>
/// <remarks>
///     This interface should be used when you only need to check if features are enabled
///     and evaluate their values. For management operations, use IFeatureFlagManagementService.
///     For configuration retrieval, use IFeatureFlagConfigurationService.
/// </remarks>
public interface IFeatureFlagEvaluationService
{
    /// <summary>
    ///     Evaluates a single feature flag for a given context
    /// </summary>
    /// <param name="featureKey">The unique key of the feature flag</param>
    /// <param name="context">The evaluation context containing user, tenant, and environment information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Evaluation result containing the feature state and value</returns>
    Task<FeatureEvaluationResult> EvaluateAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Evaluates multiple feature flags in a single request
    /// </summary>
    /// <param name="request">Bulk evaluation request containing feature keys and context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk evaluation response containing results for all requested features</returns>
    Task<BulkEvaluateFeaturesResponse> EvaluateBulkAsync(BulkEvaluationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if a feature is enabled for a specific context (simplified version)
    /// </summary>
    /// <param name="featureKey">The unique key of the feature flag</param>
    /// <param name="context">The evaluation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the feature is enabled, false otherwise</returns>
    Task<bool> IsEnabledAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the value of a feature flag with type safety
    /// </summary>
    /// <typeparam name="T">The expected type of the feature value</typeparam>
    /// <param name="featureKey">The unique key of the feature flag</param>
    /// <param name="context">The evaluation context</param>
    /// <param name="defaultValue">Default value if feature is disabled or evaluation fails</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature value or default value</returns>
    Task<T> GetValueAsync<T>(string featureKey, FeatureContext context, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all enabled features for a specific context
    /// </summary>
    /// <param name="context">The evaluation context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of enabled feature keys</returns>
    Task<IEnumerable<string>> GetEnabledFeaturesAsync(FeatureContext context, CancellationToken cancellationToken = default);
}

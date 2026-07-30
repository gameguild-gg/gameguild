

namespace GameGuild.Features;

/// <summary>
///     Service interface for managing feature flags (CRUD operations).
///     Follows Interface Segregation Principle (ISP) by focusing only on management operations.
/// </summary>
/// <remarks>
///     This interface should be used when you need to create, update, or delete feature flags.
///     For evaluation, use IFeatureFlagEvaluationService. For configuration retrieval,
///     use IFeatureFlagConfigurationService.
/// </remarks>
public interface IFeatureFlagManagementService
{
    /// <summary>
    ///     Creates a new feature flag
    /// </summary>
    /// <param name="request">The feature flag creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the created feature flag</returns>
    Task<Guid> CreateFeatureFlagAsync(CreateFeatureFlagRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing feature flag
    /// </summary>
    /// <param name="featureFlagId">The ID of the feature flag to update</param>
    /// <param name="request">The update request containing new values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateFeatureFlagAsync(Guid featureFlagId, UpdateFeatureFlagRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a feature flag
    /// </summary>
    /// <param name="featureFlagId">The ID of the feature flag to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteFeatureFlagAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Enables a feature flag
    /// </summary>
    /// <param name="featureFlagId">The ID of the feature flag to enable</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnableFeatureAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Disables a feature flag
    /// </summary>
    /// <param name="featureFlagId">The ID of the feature flag to disable</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DisableFeatureAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a targeting rule for a feature flag
    /// </summary>
    /// <param name="request">The targeting rule creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the created targeting rule</returns>
    Task<Guid> CreateTargetingRuleAsync(FeatureFlagTargetingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates a targeting rule
    /// </summary>
    /// <param name="targetId">The ID of the targeting rule to update</param>
    /// <param name="request">The update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateTargetingRuleAsync(Guid targetId, FeatureFlagTargetingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a targeting rule
    /// </summary>
    /// <param name="targetId">The ID of the targeting rule to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteTargetingRuleAsync(Guid targetId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates the rollout percentage for a feature flag
    /// </summary>
    /// <param name="featureFlagId">The ID of the feature flag</param>
    /// <param name="percentage">The new rollout percentage (0-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateRolloutPercentageAsync(Guid featureFlagId, int percentage, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a feature flag by its ID
    /// </summary>
    /// <param name="featureFlagId">The feature flag identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature flag if found, otherwise null</returns>
    Task<FeatureFlag?> GetByIdAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a feature flag by its key
    /// </summary>
    /// <param name="featureKey">The feature flag key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature flag if found, otherwise null</returns>
    Task<FeatureFlag?> GetByKeyAsync(string featureKey, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all feature flags with optional filtering
    /// </summary>
    /// <param name="environment">Optional environment filter</param>
    /// <param name="enabledOnly">If true, returns only enabled features</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of feature flags</returns>
    Task<IEnumerable<FeatureFlag>> GetAllAsync(string? environment = null, bool enabledOnly = false, CancellationToken cancellationToken = default);
}

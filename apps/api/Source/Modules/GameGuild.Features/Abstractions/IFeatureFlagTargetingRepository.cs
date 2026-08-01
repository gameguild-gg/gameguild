namespace GameGuild.Features;

/// <summary>
///     Repository interface for feature flag targeting operations.
///     Follows Interface Segregation Principle (ISP) by separating targeting concerns from CRUD operations.
/// </summary>
public interface IFeatureFlagTargetingRepository
{
    /// <summary>
    ///     Creates a new targeting rule for a feature flag
    /// </summary>
    /// <param name="target">The targeting rule to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The ID of the created targeting rule</returns>
    Task<Guid> CreateTargetAsync(FeatureFlagTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing targeting rule
    /// </summary>
    /// <param name="target">The targeting rule with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateTargetAsync(FeatureFlagTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a targeting rule
    /// </summary>
    /// <param name="targetId">The ID of the targeting rule to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteTargetAsync(Guid targetId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all targeting rules for a specific feature flag
    /// </summary>
    /// <param name="featureFlagId">The feature flag identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of targeting rules for the specified feature flag</returns>
    Task<IEnumerable<FeatureFlagTarget>> GetTargetsAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a specific targeting rule by its ID
    /// </summary>
    /// <param name="targetId">The targeting rule identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The targeting rule if found, otherwise null</returns>
    Task<FeatureFlagTarget?> GetTargetByIdAsync(Guid targetId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets targeting rules for a specific feature flag and target type
    /// </summary>
    /// <param name="featureFlagId">The feature flag identifier</param>
    /// <param name="targetType">The type of target (e.g., "tenant", "user", "plan")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of targeting rules matching the criteria</returns>
    Task<IEnumerable<FeatureFlagTarget>> GetTargetsByTypeAsync(Guid featureFlagId, string targetType, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets targeting rules for a specific feature flag and target identifier
    /// </summary>
    /// <param name="featureFlagId">The feature flag identifier</param>
    /// <param name="targetIdentifier">The target identifier (e.g., tenant ID, user ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of targeting rules matching the criteria</returns>
    Task<IEnumerable<FeatureFlagTarget>> GetTargetsByIdentifierAsync(Guid featureFlagId, string targetIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Bulk creates multiple targeting rules
    /// </summary>
    /// <param name="targets">Collection of targeting rules to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of IDs for the created targeting rules</returns>
    Task<IEnumerable<Guid>> CreateTargetsAsync(IEnumerable<FeatureFlagTarget> targets, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes all targeting rules for a specific feature flag
    /// </summary>
    /// <param name="featureFlagId">The feature flag identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteTargetsByFeatureFlagAsync(Guid featureFlagId, CancellationToken cancellationToken = default);
}

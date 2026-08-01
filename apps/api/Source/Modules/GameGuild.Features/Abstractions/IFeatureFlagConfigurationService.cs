namespace GameGuild.Features;

/// <summary>
///     Service interface for retrieving feature flag configurations.
///     Follows Interface Segregation Principle (ISP) by focusing only on configuration retrieval.
/// </summary>
/// <remarks>
///     This interface is primarily used by SDKs and client applications to retrieve
///     feature flag configurations for initialization and caching purposes.
///     For runtime evaluation, use IFeatureFlagEvaluationService.
/// </remarks>
public interface IFeatureFlagConfigurationService
{
    /// <summary>
    ///     Gets the configuration for a specific feature flag
    /// </summary>
    /// <param name="featureKey">The unique key of the feature flag</param>
    /// <param name="environment">The environment name (optional, defaults to production)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature flag configuration if found, otherwise null</returns>
    Task<FeatureFlagConfig?> GetConfigAsync(string featureKey, string? environment = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all feature flag configurations for an environment
    /// </summary>
    /// <param name="environment">The environment name (optional, defaults to production)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of all feature flag configurations</returns>
    Task<IEnumerable<FeatureFlagConfig>> GetAllConfigsAsync(string? environment = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets configurations for multiple feature flags in a single request
    /// </summary>
    /// <param name="featureKeys">Collection of feature flag keys</param>
    /// <param name="environment">The environment name (optional, defaults to production)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping feature keys to their configurations</returns>
    Task<IDictionary<string, FeatureFlagConfig>> GetConfigsAsync(IEnumerable<string> featureKeys, string? environment = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the configuration hash/version for cache validation
    /// </summary>
    /// <param name="environment">The environment name (optional, defaults to production)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hash representing the current configuration state</returns>
    Task<string> GetConfigHashAsync(string? environment = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if configurations have changed since a given hash
    /// </summary>
    /// <param name="currentHash">The current configuration hash from the client</param>
    /// <param name="environment">The environment name (optional, defaults to production)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if configurations have changed, false otherwise</returns>
    Task<bool> HasConfigChangedAsync(string currentHash, string? environment = null, CancellationToken cancellationToken = default);
}

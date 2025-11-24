using GameGuild.Abstractions;
using GameGuild.Features.DTOs;
using GameGuild.Features.Entities;
using GameGuild.Features.Models;

namespace GameGuild.Features.Abstractions;

/// <summary>
///     Repository interface for feature flag CRUD and query operations.
///     Follows Interface Segregation Principle (ISP) by containing only core data access methods.
/// </summary>
public interface IFeatureFlagQueryRepository : IRepository<FeatureFlag, Guid>
{
    /// <summary>
    ///     Gets a feature flag by its unique key
    /// </summary>
    /// <param name="key">The unique key of the feature flag</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The feature flag if found, otherwise null</returns>
    Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all feature flags that are currently enabled
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of enabled feature flags</returns>
    Task<IEnumerable<FeatureFlag>> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all feature flags for a specific environment
    /// </summary>
    /// <param name="environment">The environment name (e.g., "production", "staging")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of feature flags for the specified environment</returns>
    Task<IEnumerable<FeatureFlag>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all feature flags for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of feature flags for the specified tenant</returns>
    Task<IEnumerable<FeatureFlag>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets multiple feature flags by their keys in a single query
    /// </summary>
    /// <param name="keys">Collection of feature flag keys</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of feature flags matching the provided keys</returns>
    Task<IEnumerable<FeatureFlag>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    // Additional methods that are being called but not yet defined in interface - proper signatures
    Task<IEnumerable<FeatureFlagTargetDto>> GetTargetingRulesAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    Task<FeatureFlagTargetDto?> GetTargetingRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<IEnumerable<FeatureFlagUsageSummary>> GetUsageSummaryAsync(string featureKey, DateTime? startDate, DateTime? endDate, string? groupBy, CancellationToken cancellationToken = default);

    Task<FeatureFlagStatistics> GetStatisticsAsync(string environment, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);

    Task<PagedResult<FeatureFlagEvaluationHistory>> GetEvaluationHistoryAsync(
        string featureKey,
        DateTime? startDate,
        DateTime? endDate,
        Guid? tenantId,
        Guid? userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    Task<IEnumerable<FeatureFlagDependency>> GetDependenciesAsync(Guid featureFlagId, bool includeInverse, CancellationToken cancellationToken = default);

    Task<IEnumerable<FeatureFlagConfig>> GetConfigsAsync(string environment, string? tenantId, IEnumerable<string>? featureKeys, DateTime? modifiedSince, CancellationToken cancellationToken = default);

    Task<FeatureFlagAnalytics> GetAnalyticsAsync(string featureKey, DateTime? startDate, DateTime? endDate, string? environment, Guid? tenantId, CancellationToken cancellationToken = default);
}

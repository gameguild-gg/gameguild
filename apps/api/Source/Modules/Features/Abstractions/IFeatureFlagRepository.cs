using GameGuild;
using GameGuild.Modules.Features.Entities;

namespace GameGuild.Modules.Features.Abstractions;

/// <summary>
///     Repository interface for feature flag operations
/// </summary>
public interface IFeatureFlagRepository : IRepository<FeatureFlag, Guid>
{
    /// <summary>
    ///     Gets a feature flag by its key
    /// </summary>
    Task<FeatureFlag?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all enabled feature flags
    /// </summary>
    Task<IEnumerable<FeatureFlag>> GetEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets feature flags by environment
    /// </summary>
    Task<IEnumerable<FeatureFlag>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default);

    // Targeting operations
    Task<Guid> CreateTargetAsync(FeatureFlagTarget target, CancellationToken cancellationToken = default);

    Task UpdateTargetAsync(FeatureFlagTarget target, CancellationToken cancellationToken = default);

    Task DeleteTargetAsync(Guid targetId, CancellationToken cancellationToken = default);

    Task<IEnumerable<FeatureFlagTarget>> GetTargetsAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    // Analytics operations
    Task RecordUsageAsync(FeatureFlagUsage usage, CancellationToken cancellationToken = default);

    Task<IEnumerable<FeatureFlagUsage>> GetUsageAnalyticsAsync(string featureKey, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets feature flags for a specific tenant
    /// </summary>
    Task<IEnumerable<FeatureFlag>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}


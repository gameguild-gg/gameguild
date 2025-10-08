using GameGuild.Modules.Features.Entities;
using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Abstractions;

/// <summary>
///     Service for managing feature flags and access control
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    ///     Checks if a feature is enabled for a tenant
    /// </summary>
    Task<bool> IsEnabledAsync(string featureKey, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets feature access result with detailed information
    /// </summary>
    Task<FeatureAccessResult> GetFeatureAccessAsync(string featureKey, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Enables a feature flag
    /// </summary>
    Task EnableFeatureAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Disables a feature flag
    /// </summary>
    Task DisableFeatureAsync(Guid featureFlagId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all enabled features for a tenant
    /// </summary>
    Task<IEnumerable<FeatureFlag>> GetEnabledFeaturesAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
}


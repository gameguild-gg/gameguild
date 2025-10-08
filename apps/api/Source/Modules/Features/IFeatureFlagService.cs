using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Abstractions;

/// <summary>
///     Abstraction for feature flag evaluations. Uses OpenFeature under the hood.
/// </summary>
public interface IFeatureFlagService
{
    Task<bool> GetBooleanAsync(string key, bool defaultValue = false, EvaluationContext? context = null, CancellationToken ct = default);

    Task<string> GetStringAsync(string key, string defaultValue = "", EvaluationContext? context = null, CancellationToken ct = default);

    Task<int> GetIntAsync(string key, int defaultValue = 0, EvaluationContext? context = null, CancellationToken ct = default);

    Task<double> GetDoubleAsync(string key, double defaultValue = 0d, EvaluationContext? context = null, CancellationToken ct = default);

    /// <summary>
    ///     Checks if a feature is enabled for a tenant
    /// </summary>
    Task<bool> IsEnabledAsync(string featureKey, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets feature access result with detailed information for tenant context
    /// </summary>
    Task<TenantFeatureAccessResult> GetFeatureAccessAsync(string featureKey, Guid? tenantId = null, CancellationToken cancellationToken = default);
}


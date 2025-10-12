using GameGuild.Modules.Features.Models;

namespace GameGuild.Modules.Features.Abstractions;

/// <summary>
///     Advanced feature flag service interface
/// </summary>
public interface IAdvancedFeatureFlagService
{
    // Feature evaluation
    Task<FeatureEvaluationResult> EvaluateFeatureAsync(string featureKey, FeatureContext context, CancellationToken cancellationToken = default);

    Task<BulkEvaluationResponse> EvaluateFeaturesAsync(BulkEvaluationRequest request, CancellationToken cancellationToken = default);

    // Configuration for SDK
    Task<FeatureFlagConfig?> GetFeatureFlagConfigAsync(string featureKey, CancellationToken cancellationToken = default);

    Task<IEnumerable<FeatureFlagConfig>> GetAllFeatureFlagConfigsAsync(string environment = "production", CancellationToken cancellationToken = default);

    // Targeting management
    Task<Guid> CreateTargetingRuleAsync(FeatureFlagTargetingRequest request, CancellationToken cancellationToken = default);

    // Analytics
    Task<FeatureFlagAnalytics> GetAnalyticsAsync(string featureKey, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}


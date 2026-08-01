namespace GameGuild.Features;

/// <summary>
///     Composite repository interface that combines all feature flag repository concerns.
///     This interface provides a unified implementation for services that need all three aspects:
///     query, targeting, and analytics operations.
/// </summary>
/// <remarks>
///     This interface follows the Facade pattern and should be used for services that require
///     access to all feature flag repository operations. If you only need specific operations,
///     inject the specific interface (IFeatureFlagQueryRepository, IFeatureFlagTargetingRepository,
///     or IFeatureFlagAnalyticsRepository) to follow the Dependency Inversion Principle.
/// </remarks>
public interface IFeatureFlagRepositoryFacade : IFeatureFlagQueryRepository, IFeatureFlagTargetingRepository, IFeatureFlagAnalyticsRepository
{
    // This interface intentionally has no additional methods.
    // It combines the three segregated interfaces for convenience.
}

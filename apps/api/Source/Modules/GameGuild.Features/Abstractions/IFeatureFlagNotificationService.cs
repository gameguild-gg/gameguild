namespace GameGuild.Features;

/// <summary>
///     Feature flag notification service interface
/// </summary>
public interface IFeatureFlagNotificationService
{
    Task NotifyFeatureFlagChangedAsync(string featureKey, FeatureFlagChangeType changeType, CancellationToken cancellationToken = default);

    Task NotifyTargetingRuleChangedAsync(Guid targetId, FeatureFlagChangeType changeType, CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> GetActiveSubscriptionsAsync(string featureKey, CancellationToken cancellationToken = default);
}

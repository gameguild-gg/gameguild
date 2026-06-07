using GameGuild.CQRS;

namespace GameGuild.Features;

/// <summary>
///     Domain event raised when a feature flag is toggled
/// </summary>
public class FeatureFlagToggledEvent(Guid featureFlagId, string key, bool isEnabled, Guid? tenantId = null) : DomainEvent
{
    public Guid FeatureFlagId { get; } = featureFlagId;

    public string Key { get; } = key;

    public bool IsEnabled { get; } = isEnabled;

    public Guid? TenantId { get; } = tenantId;
}

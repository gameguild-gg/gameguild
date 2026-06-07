namespace GameGuild.Features;

/// <summary>
///     Result of feature access check
/// </summary>
public class FeatureAccessResult
{
    public bool HasAccess { get; init; }

    public string? Reason { get; init; }

    public FeatureFlag? FeatureFlag { get; init; }

    public SubscriptionPlan? Plan { get; init; }

    public bool RequiresUpgrade { get; init; }
}

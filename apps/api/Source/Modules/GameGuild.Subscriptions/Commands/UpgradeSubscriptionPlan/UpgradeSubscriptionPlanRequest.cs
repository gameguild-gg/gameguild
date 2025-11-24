namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for upgrading a subscription plan
/// </summary>
public record UpgradeSubscriptionPlanRequest(Guid NewPlanId, DateTime? EffectiveDate);

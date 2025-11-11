namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for downgrading a subscription plan
/// </summary>
public record DowngradeSubscriptionPlanRequest(Guid NewPlanId, DateTime? EffectiveDate);

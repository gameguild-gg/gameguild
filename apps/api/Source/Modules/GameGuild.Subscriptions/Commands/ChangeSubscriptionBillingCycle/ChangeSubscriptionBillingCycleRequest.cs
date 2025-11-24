namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for changing subscription billing cycle
/// </summary>
public record ChangeSubscriptionBillingCycleRequest(BillingCycle BillingCycle);

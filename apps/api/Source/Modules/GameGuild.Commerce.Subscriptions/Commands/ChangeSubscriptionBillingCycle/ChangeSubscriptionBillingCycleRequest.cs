
namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for changing subscription billing cycle
/// </summary>
public sealed record ChangeSubscriptionBillingCycleRequest(BillingCycle BillingCycle);

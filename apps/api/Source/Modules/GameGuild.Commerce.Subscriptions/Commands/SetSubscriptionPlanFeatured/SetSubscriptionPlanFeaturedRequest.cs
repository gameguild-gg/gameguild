namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for setting subscription plan as featured
/// </summary>
public sealed record SetSubscriptionPlanFeaturedRequest(bool Featured = true);

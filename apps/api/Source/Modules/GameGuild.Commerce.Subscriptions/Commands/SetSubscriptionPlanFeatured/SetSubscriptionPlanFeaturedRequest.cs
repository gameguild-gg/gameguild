namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for setting subscription plan as featured
/// </summary>
public record SetSubscriptionPlanFeaturedRequest(bool Featured = true);

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for updating plan pricing
/// </summary>
public sealed record UpdatePricingRequest(long MonthlyPriceInCents, long? AnnualPriceInCents = null);

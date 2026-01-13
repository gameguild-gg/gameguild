namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Request model for updating plan pricing
/// </summary>
public record UpdatePricingRequest(long MonthlyPriceInCents, long? AnnualPriceInCents = null);

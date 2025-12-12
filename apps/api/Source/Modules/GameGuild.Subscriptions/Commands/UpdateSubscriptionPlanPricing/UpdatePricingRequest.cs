namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Request model for updating plan pricing
/// </summary>
public record UpdatePricingRequest(long MonthlyPriceInCents, long? AnnualPriceInCents = null);

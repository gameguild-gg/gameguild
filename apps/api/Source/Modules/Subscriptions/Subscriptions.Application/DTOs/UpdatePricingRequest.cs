namespace Subscriptions.Domain.SubscriptionPlans.Models;

/// <summary>
///     Request model for updating plan pricing
/// </summary>
public record UpdatePricingRequest(long MonthlyPriceInCents, long? AnnualPriceInCents = null);


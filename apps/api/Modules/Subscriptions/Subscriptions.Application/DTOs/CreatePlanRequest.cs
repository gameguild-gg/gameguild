namespace Subscriptions.Domain.SubscriptionPlans.Models;

/// <summary>
///     Request model for creating a subscription plan
/// </summary>
public record CreatePlanRequest(string Name, string Slug, long MonthlyPriceInCents, string Currency = "USD", string? Description = null);


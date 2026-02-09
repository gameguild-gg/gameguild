using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Query to calculate pricing for a subscription plan.
/// </summary>
/// <remarks>
///     This query requires integration with the Subscriptions module for plan pricing.
///     It combines base pricing with discount codes and promo stacking rules.
/// </remarks>
public sealed record CalculatePricingQuery(Guid PlanId, Guid? TenantId = null, string? DiscountCode = null) : IQuery<PricingCalculationResult>;

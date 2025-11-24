using GameGuild.CQRS;
using GameGuild.Payments.Models;

namespace GameGuild.Payments.Queries;

/// <summary>
///     Query to calculate pricing for a subscription plan
/// </summary>
/// <remarks>
///     TODO: This query depends on the Subscriptions module.
///     Implement after Subscriptions module is integrated.
/// </remarks>
public record CalculatePricingQuery(Guid PlanId, Guid? TenantId = null, string? DiscountCode = null) : IQuery<PricingCalculationResult>;

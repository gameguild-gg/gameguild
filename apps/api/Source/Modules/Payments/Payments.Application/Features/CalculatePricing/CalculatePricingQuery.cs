using GameGuild.Modules.Payments.Models;
using MediatR;

namespace GameGuild.Modules.Payments.Features.CalculatePricing;

/// <summary>
///     Query to calculate pricing for a subscription plan
/// </summary>
/// <param name="PlanId">Subscription plan ID</param>
/// <param name="TenantId">Optional tenant ID for tenant-specific pricing</param>
/// <param name="DiscountCode">Optional discount code</param>
public record CalculatePricingQuery(
    Guid PlanId,
    Guid? TenantId = null,
    string? DiscountCode = null) : IQuery<PricingCalculationResult>;


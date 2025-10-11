using GameGuild.Modules.Products.Application.Services;
using GameGuild.CQRS;

using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.Application.Features.CalculatePricing;

/// <summary>Query to calculate dynamic pricing</summary>
public record CalculateDynamicPricingQuery(
    Guid ProductId,
    int Quantity,
    string? Region = null,
    string? CustomerSegment = null
) : IRequest<PricingCalculationResult>;

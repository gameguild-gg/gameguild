using GameGuild.Modules.Products.Application.Services;
using GameGuild.CQRS;

using ProductEntity = GameGuild.Modules.Products.Models.Product;
namespace GameGuild.Modules.Products.Application.Features.CalculatePricing;

/// <summary>Handler for calculating dynamic pricing</summary>
public class CalculateDynamicPricingHandler : IRequestHandler<CalculateDynamicPricingQuery, PricingCalculationResult>
{
    private readonly IPricingEngine _pricingEngine;

    public CalculateDynamicPricingHandler(IPricingEngine pricingEngine)
    {
        _pricingEngine = pricingEngine;
    }

    public async Task<PricingCalculationResult> Handle(CalculateDynamicPricingQuery request, CancellationToken cancellationToken)
    {
        return await _pricingEngine.CalculatePriceAsync(
            request.ProductId,
            request.Quantity,
            request.Region,
            request.CustomerSegment,
            null,
            cancellationToken);
    }
}

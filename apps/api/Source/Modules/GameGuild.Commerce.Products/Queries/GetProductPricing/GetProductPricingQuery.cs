using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query to get pricing options for a product
/// </summary>
/// <param name="ProductId">Product ID</param>
public record GetProductPricingQuery(Guid ProductId) : IQuery<IReadOnlyList<ProductPricingDto>>;

using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query to get pricing options for a product
/// </summary>
/// <param name="ProductId">Product ID</param>
/// <param name="IncludeUnpublished">Whether drafts should be visible</param>
public sealed record GetProductPricingQuery(Guid ProductId, bool IncludeUnpublished = false) : IQuery<IReadOnlyList<ProductPricingDto>>;

using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query to get a product by its ID
/// </summary>
/// <param name="ProductId">ID of the product to retrieve</param>
/// <param name="IncludePricing">Whether to include pricing information</param>
/// <param name="IncludeCreator">Whether to include creator information</param>
/// <param name="IncludeUnpublished">Whether drafts should be visible</param>
public sealed record GetProductByIdQuery(
    Guid ProductId,
    bool IncludePricing = true,
    bool IncludeCreator = false,
    bool IncludeUnpublished = false
) : IQuery<ProductDto?>;

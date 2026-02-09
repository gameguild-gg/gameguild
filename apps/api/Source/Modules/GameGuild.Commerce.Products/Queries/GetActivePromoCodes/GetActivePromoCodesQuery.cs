using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query to get all active promo codes (optionally for a specific product)
/// </summary>
/// <param name="ProductId">Optional product ID to filter by</param>
public sealed record GetActivePromoCodesQuery(Guid? ProductId = null) : IQuery<IReadOnlyList<PromoCodeDto>>;

using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query to get a promo code by ID
/// </summary>
/// <param name="Id">Promo code ID</param>
public record GetPromoCodeByIdQuery(Guid Id) : IQuery<PromoCodeDto?>;

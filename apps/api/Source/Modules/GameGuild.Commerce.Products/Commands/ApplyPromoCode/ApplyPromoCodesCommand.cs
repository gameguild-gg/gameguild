using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to apply one or more promo codes to an order
/// </summary>
/// <param name="OrderAmount">The order amount to apply codes to</param>
/// <param name="PromoCodes">List of promo codes to apply</param>
/// <param name="ProductId">Optional product ID for product-specific codes</param>
/// <param name="UserId">Optional user ID for user-specific validation</param>
public sealed record ApplyPromoCodesCommand(
    decimal OrderAmount,
    List<string> PromoCodes,
    Guid? ProductId = null,
    Guid? UserId = null
) : ICommand<PromoCodeApplicationResult>;

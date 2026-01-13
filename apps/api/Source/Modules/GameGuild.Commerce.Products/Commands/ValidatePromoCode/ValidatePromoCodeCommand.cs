using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to validate a single promo code
/// </summary>
/// <param name="Code">The promo code to validate</param>
/// <param name="OrderAmount">The order amount</param>
/// <param name="ProductId">Optional product ID</param>
/// <param name="UserId">Optional user ID</param>
public record ValidatePromoCodeCommand(
    string Code,
    decimal OrderAmount,
    Guid? ProductId = null,
    Guid? UserId = null
) : ICommand<PromoCodeValidationResult>;

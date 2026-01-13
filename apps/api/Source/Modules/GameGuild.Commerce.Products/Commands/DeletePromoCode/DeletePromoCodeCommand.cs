using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to delete a promo code
/// </summary>
/// <param name="Id">Promo code ID</param>
public record DeletePromoCodeCommand(Guid Id) : ICommand<Unit>;

using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to add a product to an existing order
/// </summary>
public sealed record AddProductToOrderCommand(
    Guid OrderId,
    Guid ProductId,
    Guid ProductPricingId,
    Guid ProductPricingVersionId,
    int Quantity = 1,
    string? PromoCode = null) : ICommand<Result<Order>>;

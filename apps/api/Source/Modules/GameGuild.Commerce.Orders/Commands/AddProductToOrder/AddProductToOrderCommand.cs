using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to add a product to an existing order
/// </summary>
public sealed record AddProductToOrderCommand(
    Guid OrderId,
    Guid ProductId,
    int Quantity = 1,
    string? PromoCode = null) : ICommand<Result<Order>>;

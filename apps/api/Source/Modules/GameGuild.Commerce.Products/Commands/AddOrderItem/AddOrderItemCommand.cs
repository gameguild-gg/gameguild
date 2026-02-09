namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to add a product to an existing order
/// </summary>
public sealed record AddOrderItemCommand(
    Guid OrderId,
    Guid ProductId,
    int Quantity = 1,
    string? PromoCode = null);

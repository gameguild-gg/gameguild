namespace GameGuild.Commerce.Orders;

/// <summary>
/// Immutable server-resolved pricing data captured when an item is added to an order.
/// </summary>
public sealed record OrderLineItemPricingSnapshot(
    Guid ProductPricingId,
    Guid ProductPricingVersionId,
    int PriceVersion,
    decimal BasePrice,
    decimal? SalePrice,
    decimal UnitPrice,
    string Currency);

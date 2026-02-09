namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to complete an order (process payment, grant entitlements)
/// </summary>
public sealed record CompleteOrderCommand(
    Guid OrderId,
    string? PaymentProviderReference = null,
    string? PaymentMethod = null);

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command to create a new order with idempotency protection
/// </summary>
public record CreateOrderCommand(
    Guid UserId,
    string IdempotencyKey,
    string Currency = "USD",
    Guid? TenantId = null,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Result of creating an order
/// </summary>
public record CreateOrderResult(
    bool Success,
    Order? Order,
    string? ErrorMessage,
    bool WasDuplicate);

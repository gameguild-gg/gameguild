namespace GameGuild.Commerce.Orders;

/// <summary>
/// Service interface for managing orders and purchases
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Create a new order with idempotency protection
    /// Returns existing order if idempotency key matches
    /// </summary>
    Task<OrderResult> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a product to an existing order
    /// </summary>
    Task<Order> AddProductToOrderAsync(
        Guid orderId,
        Guid productId,
        int quantity = 1,
        string? promoCode = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete an order (process payment, grant entitlements)
    /// </summary>
    /// <param name="orderId">The order ID to complete</param>
    /// <param name="paymentId">Optional internal Payment entity ID for Payment→Order linkage</param>
    /// <param name="paymentProviderReference">Optional external payment provider reference</param>
    /// <param name="paymentMethod">Optional payment method description</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order result with success/failure status</returns>
    Task<OrderResult> CompleteOrderAsync(
        Guid orderId,
        Guid? paymentId = null,
        string? paymentProviderReference = null,
        string? paymentMethod = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an order
    /// </summary>
    Task<bool> CancelOrderAsync(
        Guid orderId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a refund for an order
    /// </summary>
    Task<OrderResult> RefundOrderAsync(
        Guid orderId,
        decimal? amount = null,
        string reason = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order by ID
    /// </summary>
    Task<Order?> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get orders for a user
    /// </summary>
    Task<IEnumerable<Order>> GetUserOrdersAsync(
        Guid userId,
        OrderStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update order details (partial update)
    /// </summary>
    Task<OrderResult> UpdateOrderAsync(
        Guid orderId,
        UpdateOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete an order
    /// </summary>
    Task<bool> DeleteOrderAsync(
        Guid orderId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Capture payment for an authorized order
    /// </summary>
    Task<OrderResult> CaptureOrderAsync(
        Guid orderId,
        decimal? amount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Place an order on hold
    /// </summary>
    Task<OrderResult> HoldOrderAsync(
        Guid orderId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Release a held order
    /// </summary>
    Task<OrderResult> ReleaseOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to update an order
/// </summary>
public record UpdateOrderRequest(
    string? Currency = null,
    string? Notes = null,
    Dictionary<string, string>? Metadata = null);

/// <summary>
/// Request to create a new order
/// </summary>
public record CreateOrderRequest(
    Guid UserId,
    string IdempotencyKey,
    string Currency = "USD",
    Guid? TenantId = null,
    string? IpAddress = null,
    string? UserAgent = null);

/// <summary>
/// Result of an order operation
/// </summary>
public class OrderResult
{
    public bool Success { get; init; }
    public Order? Order { get; init; }
    public string? ErrorMessage { get; init; }
    public bool WasDuplicate { get; init; }

    public static OrderResult Succeeded(Order order, bool wasDuplicate = false)
        => new() { Success = true, Order = order, WasDuplicate = wasDuplicate };

    public static OrderResult Failed(string message)
        => new() { Success = false, ErrorMessage = message };
}

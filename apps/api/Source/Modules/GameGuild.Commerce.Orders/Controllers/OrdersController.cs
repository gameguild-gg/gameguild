using Asp.Versioning;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Controller for managing orders and purchases.
/// Uses <see cref="ToOrderActionResult"/> and <see cref="ToBoolActionResult"/> helpers
/// to eliminate duplicated error-handling patterns (DRY).
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/orders")]
[Authorize]
public class OrdersController(IOrderService orderService, IActorContextAccessor actorContextAccessor) : BaseApiController
{
    // ── OrderResult → ActionResult mapping (DRY) ─────────────────────────

    /// <summary>
    ///     Maps an <see cref="OrderResult"/> to an <see cref="ActionResult{OrderDto}"/>.
    ///     Returns 200 OK on success, or 400 BadRequest with a ProblemDetails body on failure.
    /// </summary>
    private ActionResult<OrderDto> ToOrderActionResult(OrderResult result)
    {
        if (!result.Success)
            return BadRequest(CreateProblemDetails(result.ErrorMessage));

        return Ok(MapToDto(result.Order!));
    }

    /// <summary>
    ///     Maps a boolean success/failure to 204 NoContent or 400 BadRequest.
    /// </summary>
    private IActionResult ToBoolActionResult(bool success, string failureMessage)
    {
        if (!success)
            return BadRequest(CreateProblemDetails(failureMessage));

        return NoContent();
    }

    /// <summary>
    ///     Creates a consistent ProblemDetails error response.
    /// </summary>
    private static ProblemDetails CreateProblemDetails(string? errorMessage) => new()
    {
        Title = "Order Operation Failed",
        Detail = errorMessage ?? "An unexpected error occurred.",
        Status = 400,
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
    };

    // ── Endpoints ────────────────────────────────────────────────────────

    /// <summary>
    /// Create a new order with idempotency protection
    /// </summary>
    [HttpPost]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<ActionResult<OrderDto>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.CreateOrderAsync(
            new CreateOrderRequest(
                request.UserId,
                request.IdempotencyKey,
                request.Currency,
                request.TenantId,
                GetIpAddress(),
                GetUserAgent()),
            cancellationToken);

        if (!result.Success)
            return BadRequest(CreateProblemDetails(result.ErrorMessage));

        var dto = MapToDto(result.Order!);

        return result.WasDuplicate
            ? Ok(dto) // Return 200 for idempotent duplicate
            : CreatedAtAction(nameof(GetOrder), new { orderId = dto.Id }, dto);
    }

    /// <summary>
    /// Add a product to an existing order
    /// </summary>
    [HttpPost("{orderId:guid}/items")]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<ActionResult<OrderDto>> AddProductToOrder(
        Guid orderId,
        [FromBody] AddOrderItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderService.AddProductToOrderAsync(
            orderId,
            request.ProductId,
            request.Quantity,
            request.PromoCode,
            cancellationToken).ConfigureAwait(false);

        return Ok(MapToDto(order));
    }

    /// <summary>
    /// Complete an order (process payment, grant entitlements)
    /// </summary>
    [HttpPost("{orderId:guid}:complete")]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<ActionResult<OrderDto>> CompleteOrder(
        Guid orderId,
        [FromBody] CompleteOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.CompleteOrderAsync(
            orderId,
            request?.PaymentId,
            request?.PaymentProviderReference,
            request?.PaymentMethod,
            cancellationToken).ConfigureAwait(false);

        return ToOrderActionResult(result);
    }

    /// <summary>
    /// Cancel a pending order
    /// </summary>
    [HttpPost("{orderId:guid}:cancel")]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<IActionResult> CancelOrder(
        Guid orderId,
        [FromBody] CancelOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var success = await orderService.CancelOrderAsync(
            orderId,
            request?.Reason,
            cancellationToken).ConfigureAwait(false);

        return ToBoolActionResult(success, "Cannot cancel order in current state");
    }

    /// <summary>
    /// Process a refund for a completed order
    /// </summary>
    [HttpPost("{orderId:guid}:refund")]
    [RequirePermission(OrdersPermission.Keys.Refund)]
    public async Task<ActionResult<OrderDto>> RefundOrder(
        Guid orderId,
        [FromBody] RefundOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.RefundOrderAsync(
            orderId,
            request.Amount,
            request.Reason ?? "",
            cancellationToken).ConfigureAwait(false);

        return ToOrderActionResult(result);
    }

    /// <summary>
    /// Get an order by ID
    /// </summary>
    [HttpGet("{orderId:guid}")]
    [RequirePermission(OrdersPermission.Keys.Read)]
    public async Task<ActionResult<OrderDto>> GetOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderService.GetOrderAsync(orderId, cancellationToken).ConfigureAwait(false);

        if (order == null)
            return NotFound();

        // Users can only view their own orders unless they have admin permission
        // This check should be enhanced with proper ownership validation
        return Ok(MapToDto(order));
    }

    /// <summary>
    /// List orders with optional filtering.
    /// Use owner=me to get current user's orders.
    /// Admin users can list all orders without owner filter.
    /// </summary>
    [HttpGet]
    [RequirePermission(OrdersPermission.Keys.Read)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> ListOrders(
        [FromQuery] string? owner = null,
        [FromQuery] OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        // Admin can list all orders when no owner filter is specified
        if (string.IsNullOrEmpty(owner) || !string.Equals(owner, "me", StringComparison.OrdinalIgnoreCase))
        {
            var allOrders = await orderService.GetAllOrdersAsync(
                status,
                cancellationToken).ConfigureAwait(false);
            return Ok(allOrders.Select(MapToDto));
        }

        // owner=me resolves to current user's orders
        var userOrders = await orderService.GetUserOrdersAsync(
            GetUserId(),
            status,
            cancellationToken).ConfigureAwait(false);

        return Ok(userOrders.Select(MapToDto));
    }

    /// <summary>
    /// Check if an order exists
    /// </summary>
    [HttpHead("{orderId:guid}")]
    [RequirePermission(OrdersPermission.Keys.Read)]
    public async Task<IActionResult> OrderExists(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderService.GetOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
        return order != null ? Ok() : NotFound();
    }

    /// <summary>
    /// Update an order (partial update)
    /// </summary>
    [HttpPatch("{orderId:guid}")]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<ActionResult<OrderDto>> UpdateOrder(
        Guid orderId,
        [FromBody] PatchOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.UpdateOrderAsync(
            orderId,
            new UpdateOrderRequest(request.Currency, request.Notes, request.Metadata),
            cancellationToken).ConfigureAwait(false);

        return ToOrderActionResult(result);
    }

    /// <summary>
    /// Delete an order (soft delete)
    /// </summary>
    [HttpDelete("{orderId:guid}")]
    [RequirePermission(OrdersPermission.Keys.Delete)]
    public async Task<IActionResult> DeleteOrder(
        Guid orderId,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var success = await orderService.DeleteOrderAsync(orderId, reason, cancellationToken).ConfigureAwait(false);

        return ToBoolActionResult(success, "Cannot delete order in current state");
    }

    /// <summary>
    /// Capture payment for an authorized order
    /// </summary>
    [HttpPost("{orderId:guid}:capture")]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<ActionResult<OrderDto>> CaptureOrder(
        Guid orderId,
        [FromBody] CaptureOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.CaptureOrderAsync(
            orderId,
            request?.Amount,
            cancellationToken).ConfigureAwait(false);

        return ToOrderActionResult(result);
    }

    /// <summary>
    /// Place an order on hold
    /// </summary>
    [HttpPost("{orderId:guid}:hold")]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<ActionResult<OrderDto>> HoldOrder(
        Guid orderId,
        [FromBody] HoldOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.HoldOrderAsync(
            orderId,
            request?.Reason,
            cancellationToken).ConfigureAwait(false);

        return ToOrderActionResult(result);
    }

    /// <summary>
    /// Release a held order
    /// </summary>
    [HttpPost("{orderId:guid}:release")]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<ActionResult<OrderDto>> ReleaseOrder(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.ReleaseOrderAsync(orderId, cancellationToken).ConfigureAwait(false);

        return ToOrderActionResult(result);
    }

    // ── Private helpers ──────────────────────────────────────────────────

    private Guid GetUserId()
    {
        return actorContextAccessor.ActorContext.SubjectIdAsGuid ?? Guid.Empty;
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string GetUserAgent()
    {
        return HttpContext.Request.Headers.UserAgent.ToString();
    }

    private static OrderDto MapToDto(Order order) => new(
        order.Id,
        order.UserId,
        order.IdempotencyKey,
        order.Status,
        order.Subtotal,
        order.DiscountTotal,
        order.TaxAmount,
        order.Total,
        order.Currency,
        order.PaymentProviderReference,
        order.PaymentMethod,
        order.PaidAt,
        order.RefundedAt,
        order.RefundAmount,
        order.RefundReason,
        order.CreatedAt,
        order.UpdatedAt,
        order.LineItems.Select(li => new OrderLineItemDto(
            li.Id,
            li.ProductId,
            li.ProductNameSnapshot,
            li.UnitPriceSnapshot,
            li.BasePriceSnapshot,
            li.SalePriceSnapshot,
            li.Quantity,
            li.DiscountAmount,
            li.PromoCodesApplied,
            li.LineTotal,
            li.IsSubscription
        )).ToList()
    );
}

/// <summary>Request to add an item to an order</summary>
public sealed record AddOrderItemRequest(
    Guid ProductId,
    int Quantity = 1,
    string? PromoCode = null);

/// <summary>Request to complete an order</summary>
/// <param name="PaymentId">Optional internal Payment entity ID for Payment→Order linkage</param>
/// <param name="PaymentProviderReference">Optional external payment provider reference</param>
/// <param name="PaymentMethod">Optional payment method description (e.g., "card", "bank_transfer")</param>
public sealed record CompleteOrderRequest(
    Guid? PaymentId = null,
    string? PaymentProviderReference = null,
    string? PaymentMethod = null);

/// <summary>Request to cancel an order</summary>
public sealed record CancelOrderRequest(string? Reason = null);

/// <summary>Request to refund an order</summary>
public sealed record RefundOrderRequest(decimal? Amount = null, string? Reason = null);

/// <summary>Order DTO</summary>
public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    string IdempotencyKey,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxAmount,
    decimal Total,
    string Currency,
    string? PaymentProviderReference,
    string? PaymentMethod,
    DateTime? PaidAt,
    DateTime? RefundedAt,
    decimal? RefundAmount,
    string? RefundReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderLineItemDto> LineItems);

/// <summary>Order line item DTO</summary>
public sealed record OrderLineItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    decimal BasePrice,
    decimal? SalePrice,
    int Quantity,
    decimal DiscountAmount,
    string? PromoCodesApplied,
    decimal LineTotal,
    bool IsSubscription);

/// <summary>Request to partially update an order</summary>
public sealed record PatchOrderRequest(
    string? Currency = null,
    string? Notes = null,
    Dictionary<string, string>? Metadata = null);

/// <summary>Request to capture payment for an order</summary>
public sealed record CaptureOrderRequest(decimal? Amount = null);

/// <summary>Request to hold an order</summary>
public sealed record HoldOrderRequest(string? Reason = null);

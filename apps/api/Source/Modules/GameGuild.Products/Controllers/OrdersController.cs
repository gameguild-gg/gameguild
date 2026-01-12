using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Products;

/// <summary>
/// Controller for managing orders and purchases
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController(IOrderService orderService) : ControllerBase
{
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
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        var dto = MapToDto(result.Order!);

        if (result.WasDuplicate)
        {
            return Ok(dto); // Return 200 for idempotent duplicate
        }

        return CreatedAtAction(nameof(GetOrder), new { orderId = dto.Id }, dto);
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
    [HttpPost("{orderId:guid}/complete")]
    [RequirePermission(OrdersPermission.Keys.Create)]
    public async Task<ActionResult<OrderDto>> CompleteOrder(
        Guid orderId,
        [FromBody] CompleteOrderRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await orderService.CompleteOrderAsync(
            orderId,
            request?.PaymentProviderReference,
            request?.PaymentMethod,
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(MapToDto(result.Order!));
    }

    /// <summary>
    /// Cancel a pending order
    /// </summary>
    [HttpPost("{orderId:guid}/cancel")]
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

        if (!success)
        {
            return BadRequest(new { error = "Cannot cancel order in current state" });
        }

        return NoContent();
    }

    /// <summary>
    /// Process a refund for a completed order
    /// </summary>
    [HttpPost("{orderId:guid}/refund")]
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

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(MapToDto(result.Order!));
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
        {
            return NotFound();
        }

        // Users can only view their own orders unless they have admin permission
        // This check should be enhanced with proper ownership validation
        return Ok(MapToDto(order));
    }

    /// <summary>
    /// Get orders for the current user
    /// </summary>
    [HttpGet("my-orders")]
    [RequirePermission(OrdersPermission.Keys.Read)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetMyOrders(
        [FromQuery] OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await orderService.GetUserOrdersAsync(
            GetUserId(),
            status,
            cancellationToken).ConfigureAwait(false);

        return Ok(orders.Select(MapToDto));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
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
public record AddOrderItemRequest(
    Guid ProductId,
    int Quantity = 1,
    string? PromoCode = null);

/// <summary>Request to complete an order</summary>
public record CompleteOrderRequest(
    string? PaymentProviderReference = null,
    string? PaymentMethod = null);

/// <summary>Request to cancel an order</summary>
public record CancelOrderRequest(string? Reason = null);

/// <summary>Request to refund an order</summary>
public record RefundOrderRequest(decimal? Amount = null, string? Reason = null);

/// <summary>Order DTO</summary>
public record OrderDto(
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
public record OrderLineItemDto(
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

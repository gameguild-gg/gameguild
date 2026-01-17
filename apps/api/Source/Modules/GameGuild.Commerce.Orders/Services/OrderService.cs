using GameGuild.Abstractions;
using GameGuild.Commerce.Products;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Service for managing orders and purchases with idempotency, price snapshotting, and transaction boundaries
/// </summary>
public class OrderService(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IProductPricingRepository pricingRepository,
    IPromoCodeService promoCodeService,
    IEntitlementService entitlementService,
    IApplicationDbContext dbContext) : IOrderService
{
    /// <inheritdoc />
    public async Task<OrderResult> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        // Check for existing order with same idempotency key
        var existingOrder = await orderRepository.GetByIdempotencyKeyAsync(
            request.IdempotencyKey, cancellationToken).ConfigureAwait(false);

        if (existingOrder != null)
        {
            return OrderResult.Succeeded(existingOrder, wasDuplicate: true);
        }

        // Create new order
        var order = Order.Create(
            request.UserId,
            request.IdempotencyKey,
            request.TenantId ?? Guid.Empty,
            request.Currency,
            request.IpAddress,
            request.UserAgent);

        await orderRepository.AddAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OrderResult.Succeeded(order);
    }

    /// <inheritdoc />
    public async Task<Order> AddProductToOrderAsync(
        Guid orderId,
        Guid productId,
        int quantity = 1,
        string? promoCode = null,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetWithLineItemsAsync(orderId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot add items to order in {order.Status} status");
        }

        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Product {productId} not found");

        // Get active pricing (use default pricing option)
        var pricingsList = (await pricingRepository.GetByProductIdAsync(productId, cancellationToken).ConfigureAwait(false)).ToList();
        var pricing = pricingsList.Find(p => p.IsDefault) ?? pricingsList.FirstOrDefault();
        var price = pricing?.SalePrice ?? pricing?.BasePrice ?? 0;
        var basePrice = pricing?.BasePrice ?? price;

        // Calculate discount if promo code provided
        decimal discountAmount = 0;
        string? promoCodesApplied = null;
        if (!string.IsNullOrEmpty(promoCode))
        {
            var promo = await promoCodeService.GetPromoCodeByCodeAsync(promoCode).ConfigureAwait(false);
            if (promo != null && await promoCodeService.ValidatePromoCodeAsync(promo.Code, order.UserId, productId).ConfigureAwait(false))
            {
                discountAmount = promo.Type == PromoCodeType.PercentageOff
                    ? price * quantity * (promo.DiscountPercentage ?? 0) / 100m
                    : (promo.DiscountAmount ?? 0) * quantity;
                promoCodesApplied = System.Text.Json.JsonSerializer.Serialize(new[] { promo.Code });
            }
        }

        // Add line item with price snapshot
        var lineItem = order.AddLineItem(
            productId,
            product.Name,
            price,
            quantity,
            discountAmount,
            promoCodesApplied);

        lineItem.BasePriceSnapshot = basePrice;
        lineItem.SalePriceSnapshot = pricing?.SalePrice;
        lineItem.PricingTierId = pricing?.Id;
        lineItem.PricingTierNameSnapshot = pricing?.Name;
        lineItem.IsSubscription = product.Type == ProductType.Subscription;

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return order;
    }

    /// <inheritdoc />
    public async Task<OrderResult> CompleteOrderAsync(
        Guid orderId,
        Guid? paymentId = null,
        string? paymentProviderReference = null,
        string? paymentMethod = null,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetWithLineItemsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return OrderResult.Failed($"Order {orderId} not found");
        }

        // Idempotent: already completed or fulfilled
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Fulfilled)
        {
            return OrderResult.Succeeded(order, wasDuplicate: true);
        }

        // Idempotent: already paid but not fulfilled - continue to fulfillment
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing && order.Status != OrderStatus.Paid)
        {
            return OrderResult.Failed($"Cannot complete order in {order.Status} status");
        }

        // Use explicit transaction boundary to ensure atomicity of order completion
        // This prevents partial state if any operation fails (e.g., entitlement granted but order not marked paid)
        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            // STEP 1: Mark as paid (if not already paid)
            // Economic Causality: Payment confirmation happens BEFORE fulfillment
            if (order.Status != OrderStatus.Paid)
            {
                if (paymentId.HasValue)
                {
                    // New flow: Link to internal Payment entity with proper Paid state
                    order.MarkAsPaidPendingFulfillment(paymentId.Value, paymentProviderReference);
                }
                else
                {
                    // Legacy flow: No internal Payment entity, use external reference only
                    // Still use proper two-step flow for new orders
                    order.MarkAsPaid(paymentProviderReference, paymentMethod, paymentProviderReference);
                }
            }

            // STEP 2: Grant entitlements (after payment is confirmed)
            foreach (var lineItem in order.LineItems)
            {
                var acquisitionType = lineItem.IsSubscription
                    ? ProductAcquisitionType.Subscription
                    : ProductAcquisitionType.Purchase;

                var entitlementResult = await entitlementService.GrantEntitlementAsync(
                    order.UserId,
                    lineItem.ProductId,
                    acquisitionType,
                    lineItem.LineTotal,
                    order.Currency,
                    expiresAt: null, // Will be set by subscription logic if applicable
                    orderId: order.Id,
                    cancellationToken).ConfigureAwait(false);

                if (entitlementResult.Success && entitlementResult.UserProduct != null)
                {
                    lineItem.UserProductId = entitlementResult.UserProduct.Id;
                }
            }

            // STEP 3: Mark as fulfilled (after entitlements are granted)
            // Economic Causality: Fulfillment happens AFTER entitlements are created
            order.MarkAsFulfilled();

            await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
            await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return OrderResult.Succeeded(order);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CancelOrderAsync(
        Guid orderId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return false;
        }

        if (order.Status != OrderStatus.Pending)
        {
            return false;
        }

        order.Cancel(reason);
        order.Touch();

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<OrderResult> RefundOrderAsync(
        Guid orderId,
        decimal? amount = null,
        string reason = "",
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetWithLineItemsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return OrderResult.Failed($"Order {orderId} not found");
        }

        if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.PartiallyRefunded)
        {
            return OrderResult.Failed($"Cannot refund order in {order.Status} status");
        }

        var refundAmount = amount ?? order.Total;

        // Revoke entitlements if full refund
        if (refundAmount >= order.Total)
        {
            foreach (var lineItem in order.LineItems)
            {
                await entitlementService.RevokeEntitlementAsync(
                    order.UserId,
                    lineItem.ProductId,
                    "Order refunded",
                    cancellationToken).ConfigureAwait(false);
            }
        }

        order.ProcessRefund(refundAmount, reason);

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OrderResult.Succeeded(order);
    }

    /// <inheritdoc />
    public async Task<Order?> GetOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await orderRepository.GetWithLineItemsAsync(orderId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Order>> GetUserOrdersAsync(
        Guid userId,
        OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return await orderRepository.GetByUserIdAsync(userId, status, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OrderResult> UpdateOrderAsync(
        Guid orderId,
        UpdateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return OrderResult.Failed($"Order {orderId} not found");
        }

        if (order.Status != OrderStatus.Pending)
        {
            return OrderResult.Failed($"Cannot update order in {order.Status} status");
        }

        if (request.Currency != null)
        {
            order.Currency = request.Currency;
        }

        order.Touch();

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OrderResult.Succeeded(order);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteOrderAsync(
        Guid orderId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return false;
        }

        // Only allow deletion of pending or cancelled orders
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Cancelled)
        {
            return false;
        }

        // Soft delete using proper method
        order.SoftDelete();

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<OrderResult> CaptureOrderAsync(
        Guid orderId,
        decimal? amount = null,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetWithLineItemsAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return OrderResult.Failed($"Order {orderId} not found");
        }

        // Order must be in authorized/pending state
        if (order.Status != OrderStatus.Pending)
        {
            return OrderResult.Failed($"Cannot capture order in {order.Status} status");
        }

        // Mark as completed (captured) using proper state machine method
        order.MarkAsPaid(null, null, null);

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OrderResult.Succeeded(order);
    }

    /// <inheritdoc />
    public async Task<OrderResult> HoldOrderAsync(
        Guid orderId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return OrderResult.Failed($"Order {orderId} not found");
        }

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
        {
            return OrderResult.Failed($"Cannot hold order in {order.Status} status");
        }

        // Use proper state machine method
        order.PlaceOnHold(reason);

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OrderResult.Succeeded(order);
    }

    /// <inheritdoc />
    public async Task<OrderResult> ReleaseOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null)
        {
            return OrderResult.Failed($"Order {orderId} not found");
        }

        if (order.Status != OrderStatus.OnHold)
        {
            return OrderResult.Failed($"Cannot release order in {order.Status} status");
        }

        // Use proper state machine method
        order.Release();

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return OrderResult.Succeeded(order);
    }
}

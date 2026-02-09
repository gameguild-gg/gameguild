using GameGuild.Commerce.Products;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for adding a product to an existing order
/// </summary>
public sealed class AddProductToOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IProductPricingRepository pricingRepository,
    IPromoCodeService promoCodeService)
    : ICommandHandler<AddProductToOrderCommand, Result<Order>>
{
    public async Task<Result<Order>> Handle(
        AddProductToOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetWithLineItemsAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<Order>(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        }

        if (order.Status != OrderStatus.Pending)
        {
            return Result.Failure<Order>(Error.Failure("Orders.InvalidStatus", $"Cannot add items to order in {order.Status} status"));
        }

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return Result.Failure<Order>(Error.NotFound("Products.NotFound", $"Product {request.ProductId} not found"));
        }

        // Get active pricing (use default pricing option)
        var pricingsList = (await pricingRepository.GetByProductIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)).ToList();
        var pricing = pricingsList.Find(p => p.IsDefault) ?? pricingsList.FirstOrDefault();
        var price = pricing?.SalePrice ?? pricing?.BasePrice ?? 0;
        var basePrice = pricing?.BasePrice ?? price;

        // Calculate discount if promo code provided
        decimal discountAmount = 0;
        string? promoCodesApplied = null;
        if (!string.IsNullOrEmpty(request.PromoCode))
        {
            var promo = await promoCodeService.GetPromoCodeByCodeAsync(request.PromoCode).ConfigureAwait(false);
            if (promo != null && await promoCodeService.ValidatePromoCodeAsync(promo.Code, order.UserId, request.ProductId).ConfigureAwait(false))
            {
                discountAmount = promo.Type == PromoCodeType.PercentageOff
                    ? price * request.Quantity * (promo.DiscountPercentage ?? 0) / 100m
                    : (promo.DiscountAmount ?? 0) * request.Quantity;
                promoCodesApplied = System.Text.Json.JsonSerializer.Serialize(new[] { promo.Code });
            }
        }

        // Add line item with price snapshot
        var lineItem = order.AddLineItem(
            request.ProductId,
            product.Name,
            price,
            request.Quantity,
            discountAmount,
            promoCodesApplied);

        lineItem.BasePriceSnapshot = basePrice;
        lineItem.SalePriceSnapshot = pricing?.SalePrice;
        lineItem.PricingTierId = pricing?.Id;
        lineItem.PricingTierNameSnapshot = pricing?.Name;
        lineItem.IsSubscription = product.Type == ProductType.Subscription;

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(order);
    }
}

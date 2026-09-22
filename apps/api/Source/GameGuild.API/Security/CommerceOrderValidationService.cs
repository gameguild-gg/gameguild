using GameGuild.Assets.Security;
using GameGuild.Commerce.Orders;
using AssetsOrderStatus = GameGuild.Assets.Security.OrderStatus;
using CommerceOrderStatus = GameGuild.Commerce.Orders.OrderStatus;

namespace GameGuild.API.Security;

/// <summary>
///     Commerce-backed order validation for paid asset downloads.
/// </summary>
/// <remarks>
///     Composition-root adapter: the Assets module exposes only the generic
///     <see cref="IOrderValidationService"/> abstraction and a fail-closed default; this
///     product-specific implementation binds that abstraction to the commerce order
///     repository so platform modules never depend on the commerce stack directly.
/// </remarks>
public sealed class CommerceOrderValidationService(IOrderRepository orderRepository) : IOrderValidationService
{
    public async Task<AssetsOrderStatus?> GetOrderStatusAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct).ConfigureAwait(false);
        return order == null ? null : MapStatus(order.Status);
    }

    public async Task<bool> IsOrderValidForDownloadAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, ct).ConfigureAwait(false);
        return order?.Status is CommerceOrderStatus.Paid or CommerceOrderStatus.Fulfilled or CommerceOrderStatus.Completed;
    }

    private static AssetsOrderStatus MapStatus(CommerceOrderStatus status)
        => status switch
        {
            CommerceOrderStatus.Paid => AssetsOrderStatus.Paid,
            CommerceOrderStatus.Fulfilled or CommerceOrderStatus.Completed => AssetsOrderStatus.Fulfilled,
            CommerceOrderStatus.Refunded or CommerceOrderStatus.PartiallyRefunded => AssetsOrderStatus.Refunded,
            CommerceOrderStatus.Cancelled => AssetsOrderStatus.Cancelled,
            CommerceOrderStatus.Disputed => AssetsOrderStatus.Disputed,
            _ => AssetsOrderStatus.Pending
        };
}

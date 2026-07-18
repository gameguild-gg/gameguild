namespace GameGuild.Commerce.Orders;

internal sealed class DenyOrderPaymentAuthority : IOrderPaymentAuthority
{
    public Task<bool> IsSettledAsync(OrderPaymentBinding binding, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}

internal sealed class DenyOrderPaymentProcessor : IOrderPaymentProcessor
{
    public Task<OrderChargeResult> ProcessAsync(
        AuthoritativeOrderCharge charge,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(OrderChargeResult.Failed(null, "Payments is not configured for order charging."));
}

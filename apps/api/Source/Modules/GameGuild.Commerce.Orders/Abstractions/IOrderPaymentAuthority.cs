namespace GameGuild.Commerce.Orders;

internal sealed class DenyOrderPaymentAuthority : IOrderPaymentAuthority
{
    public Task<bool> IsSettledAsync(OrderPaymentBinding binding, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}

internal sealed class DenyOrderPaymentProcessor : IOrderPaymentProcessor
{
    public string? GetPaymentMethodValidationError(string paymentMethodId) =>
        "Payments is not configured for order charging.";

    public Task<OrderChargeResult> ProcessAsync(
        AuthoritativeOrderCharge charge,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(OrderChargeResult.Failed(null, "Payments is not configured for order charging."));
}

internal sealed class DenyOrderPaymentIntentPreparer : IOrderPaymentIntentPreparer
{
    public Task<OrderPaymentIntentPreparation> PrepareAsync(
        AuthoritativeOrderPaymentIntent intent,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new OrderPaymentIntentPreparation(
            false,
            null,
            null,
            "Payments is not configured for Payment Element setup.",
            OrderChargeState.Failed));
}

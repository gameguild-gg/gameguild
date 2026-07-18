namespace GameGuild.Commerce.Orders;

/// <summary>
/// Payments-owned verification boundary required before Orders may fulfill a paid order.
/// </summary>
public interface IOrderPaymentAuthority
{
    Task<bool> IsSettledAsync(OrderPaymentBinding binding, CancellationToken cancellationToken = default);
}

/// <summary>
/// Immutable order facts that Payments must match against its authoritative settlement binding.
/// </summary>
public sealed record OrderPaymentBinding(
    Guid OrderId,
    Guid PaymentId,
    Guid UserId,
    Guid TenantId,
    decimal Amount,
    string Currency);

internal sealed class DenyOrderPaymentAuthority : IOrderPaymentAuthority
{
    public Task<bool> IsSettledAsync(OrderPaymentBinding binding, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}

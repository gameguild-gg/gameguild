namespace GameGuild.Commerce;

/// <summary>
/// Immutable server-owned order facts supplied to Payments for one charge attempt.
/// </summary>
public sealed record AuthoritativeOrderCharge(
    Guid OrderId,
    Guid TenantId,
    decimal Amount,
    string Currency,
    string PaymentMethodId);

public enum OrderChargeState
{
    Succeeded,
    Failed,
    Processing,
    RequiresAction,
    RequiresReconciliation
}

/// <summary>
/// Result returned by the payment authority without exposing provider-controlled pricing.
/// </summary>
public sealed record OrderChargeResult(
    bool Success,
    Guid? PaymentId,
    string? ExternalPaymentId,
    string? FailureReason,
    OrderChargeState State,
    string? ClientActionToken = null)
{
    public static OrderChargeResult Succeeded(Guid paymentId, string? externalPaymentId) =>
        new(true, paymentId, externalPaymentId, null, OrderChargeState.Succeeded);

    public static OrderChargeResult Failed(Guid? paymentId, string failureReason) =>
        new(false, paymentId, null, failureReason, OrderChargeState.Failed);

    public static OrderChargeResult Processing(Guid paymentId, string message) =>
        new(false, paymentId, null, message, OrderChargeState.Processing);

    public static OrderChargeResult RequiresAction(Guid paymentId, string message, string? clientActionToken) =>
        new(false, paymentId, null, message, OrderChargeState.RequiresAction, clientActionToken);

    public static OrderChargeResult RequiresReconciliation(Guid paymentId, string message) =>
        new(false, paymentId, null, message, OrderChargeState.RequiresReconciliation);
}

/// <summary>
/// Payments-owned boundary used by Orders to charge an authoritative order snapshot.
/// </summary>
public interface IOrderPaymentProcessor
{
    /// <summary>
    /// Returns a client-safe validation error, or <see langword="null"/> when the reference is supported.
    /// </summary>
    string? GetPaymentMethodValidationError(string paymentMethodId);

    Task<OrderChargeResult> ProcessAsync(
        AuthoritativeOrderCharge charge,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Immutable settlement facts Orders requires before fulfillment.
/// </summary>
public sealed record OrderPaymentBinding(
    Guid OrderId,
    Guid PaymentId,
    Guid TenantId,
    decimal Amount,
    string Currency);

/// <summary>
/// Payments-owned verification boundary required before Orders may fulfill a paid order.
/// </summary>
public interface IOrderPaymentAuthority
{
    Task<bool> IsSettledAsync(
        OrderPaymentBinding binding,
        CancellationToken cancellationToken = default);
}

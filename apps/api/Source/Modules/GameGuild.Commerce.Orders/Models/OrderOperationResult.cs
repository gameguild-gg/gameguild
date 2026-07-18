namespace GameGuild.Commerce.Orders;

/// <summary>
/// Result DTO for order operations, carrying the order and duplicate flag.
/// Replaces the module-specific OrderResult in favor of SharedKernel Result{T}.
/// </summary>
public sealed record OrderOperationResult(
    Order Order,
    bool WasDuplicate = false,
    OrderChargeState? PaymentState = null,
    Guid? PaymentId = null,
    string? ClientActionToken = null,
    string? PaymentMessage = null)
{
    /// <summary>Create from an order entity</summary>
    public static OrderOperationResult FromOrder(Order order, bool wasDuplicate = false)
        => new(order, wasDuplicate);

    public static OrderOperationResult FromPendingPayment(Order order, OrderChargeResult chargeResult) =>
        new(
            order,
            PaymentState: chargeResult.State,
            PaymentId: chargeResult.PaymentId,
            ClientActionToken: chargeResult.ClientActionToken,
            PaymentMessage: chargeResult.FailureReason);
}

using GameGuild.Commerce;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
/// Processes and verifies payments bound to immutable order snapshots.
/// </summary>
public sealed class OrderPaymentService(
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    ILogger<OrderPaymentService> logger) : IOrderPaymentProcessor, IOrderPaymentAuthority
{
    public string? GetPaymentMethodValidationError(string paymentMethodId) =>
        StripePaymentMethodIdentifier.IsValid(paymentMethodId)
            ? null
            : StripePaymentMethodIdentifier.ValidationMessage;

    public async Task<OrderChargeResult> ProcessAsync(
        AuthoritativeOrderCharge charge,
        CancellationToken cancellationToken = default)
    {
        Validate(charge);
        var idempotencyKey = CreateIdempotencyKey(charge.TenantId, charge.OrderId);
        var existingPayment = await paymentRepository
            .GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        if (existingPayment is not null)
        {
            return await ProcessExistingAsync(existingPayment, charge, cancellationToken).ConfigureAwait(false);
        }

        var proposedPayment = Payment.Create(
            charge.TenantId,
            charge.Amount,
            charge.Currency,
            idempotencyKey,
            paymentGateway.ProviderId,
            orderId: charge.OrderId,
            paymentMethodId: charge.PaymentMethodId,
            description: $"Payment for order {charge.OrderId}");
        var payment = await paymentRepository.AddAsync(proposedPayment, cancellationToken).ConfigureAwait(false);
        if (payment.Id != proposedPayment.Id)
            return await ProcessExistingAsync(payment, charge, cancellationToken).ConfigureAwait(false);

        return await ProcessAttemptAsync(payment, charge, idempotencyKey, cancellationToken).ConfigureAwait(false);
    }

    private async Task<OrderChargeResult> ProcessExistingAsync(
        Payment payment,
        AuthoritativeOrderCharge charge,
        CancellationToken cancellationToken)
    {
        EnsureMatches(payment, charge);

        if (payment.Status == PaymentStatus.Succeeded)
            return OrderChargeResult.Succeeded(payment.Id, payment.ExternalPaymentId);

        if (payment.Status == PaymentStatus.RequiresAction)
        {
            payment.MarkAsFailed("Additional payment action was not completed before retry.", "requires_action_replaced");
        }

        if (payment.Status == PaymentStatus.Failed && payment.CanRetry)
        {
            payment.PrepareForRetry(charge.PaymentMethodId);
            await paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            return await ProcessAttemptAsync(
                    payment,
                    charge,
                    $"{payment.IdempotencyKey}:retry:{payment.RetryCount}",
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return OrderChargeResult.Failed(
            payment.Id,
            payment.FailureReason ?? $"Payment is {payment.Status}; retry after its state changes.");
    }

    private async Task<OrderChargeResult> ProcessAttemptAsync(
        Payment payment,
        AuthoritativeOrderCharge charge,
        string gatewayIdempotencyKey,
        CancellationToken cancellationToken)
    {
        payment.MarkAsProcessing();
        await paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);

        var gatewayResult = await paymentGateway.ProcessPaymentAsync(
            new GatewayPaymentRequest(
                gatewayIdempotencyKey,
                charge.Amount,
                charge.Currency,
                CustomerId: null,
                charge.PaymentMethodId,
                $"Payment for order {charge.OrderId}",
                new Dictionary<string, string>
                {
                    ["tenant_id"] = charge.TenantId.ToString(),
                    ["order_id"] = charge.OrderId.ToString()
                }),
            cancellationToken).ConfigureAwait(false);

        if (gatewayResult.Success)
        {
            payment.MarkAsSucceeded(
                gatewayResult.ExternalPaymentId ?? gatewayResult.TransactionId ?? Guid.NewGuid().ToString(),
                gatewayResult.TransactionId);
        }
        else if (gatewayResult.Status == PaymentStatus.RequiresAction)
        {
            payment.MarkAsRequiresAction(gatewayResult.TransactionId);
        }
        else
        {
            payment.MarkAsFailed(
                gatewayResult.ErrorMessage ?? "Order payment failed.",
                gatewayResult.ErrorCode);
        }

        await paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
        logger.LogInformation(
            "Order payment {PaymentId} for order {OrderId} finished with status {Status}",
            payment.Id,
            charge.OrderId,
            payment.Status);

        return gatewayResult.Success
            ? OrderChargeResult.Succeeded(payment.Id, payment.ExternalPaymentId)
            : OrderChargeResult.Failed(payment.Id, payment.FailureReason ?? gatewayResult.ErrorMessage ?? "Order payment failed.");
    }

    public async Task<bool> IsSettledAsync(
        OrderPaymentBinding binding,
        CancellationToken cancellationToken = default)
    {
        var payment = await paymentRepository.GetByIdAsync(binding.PaymentId, cancellationToken).ConfigureAwait(false);
        return payment is not null &&
               payment.Status == PaymentStatus.Succeeded &&
               payment.OrderId == binding.OrderId &&
               payment.TenantId == binding.TenantId &&
               payment.Amount == binding.Amount &&
               string.Equals(payment.Currency, binding.Currency, StringComparison.Ordinal);
    }

    internal static string CreateIdempotencyKey(Guid tenantId, Guid orderId) =>
        $"order:{tenantId:N}:{orderId:N}:charge";

    private static void Validate(AuthoritativeOrderCharge charge)
    {
        if (charge.OrderId == Guid.Empty || charge.TenantId == Guid.Empty)
            throw new ArgumentException("Order and tenant identifiers are required.", nameof(charge));
        if (charge.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(charge), "Order amount must be positive.");
        if (charge.Currency.Length != 3 || !charge.Currency.All(char.IsAsciiLetterUpper))
            throw new ArgumentException("Currency must be a three-letter uppercase code.", nameof(charge));
        var paymentMethodError = StripePaymentMethodIdentifier.IsValid(charge.PaymentMethodId)
            ? null
            : StripePaymentMethodIdentifier.ValidationMessage;
        if (paymentMethodError is not null)
            throw new ArgumentException(paymentMethodError, nameof(charge));
    }

    private static void EnsureMatches(Payment payment, AuthoritativeOrderCharge charge)
    {
        if (payment.OrderId != charge.OrderId ||
            payment.TenantId != charge.TenantId ||
            payment.Amount != charge.Amount ||
            !string.Equals(payment.Currency, charge.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Existing order payment does not match the authoritative order snapshot.");
        }
    }
}

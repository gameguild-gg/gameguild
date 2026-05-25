using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for processing payment commands
/// </summary>
public sealed class ProcessPaymentCommandHandler(
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    IPaymentSubscriptionSyncService paymentSubscriptionSyncService,
    ILogger<ProcessPaymentCommandHandler> logger) : ICommandHandler<ProcessPaymentCommand, PaymentResult>
{
    public async Task<PaymentResult> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // Generate idempotency key from request parameters
        var idempotencyKey = $"pay_{request.TenantId}_{request.SubscriptionId}_{request.Amount}_{SystemClock.UtcNow:yyyyMMddHH}";

        logger.LogInformation(
            "Processing payment for tenant {TenantId}, subscription {SubscriptionId}, amount {Amount}",
            request.TenantId, request.SubscriptionId, request.Amount);

        // 1. Check for existing payment with same idempotency key (idempotency check)
        var existingPayment = await paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        if (existingPayment != null)
        {
            logger.LogInformation("Payment already exists with idempotency key {IdempotencyKey}, returning existing result",
                idempotencyKey);

            return new PaymentResult
            {
                Success = existingPayment.Status == PaymentStatus.Succeeded,
                TransactionId = existingPayment.ExternalTransactionId,
                PaymentId = existingPayment.Id.ToString(),
                Amount = new Money(existingPayment.Amount, existingPayment.Currency),
                ProcessedAt = existingPayment.ProcessedAt,
                Status = existingPayment.Status,
                FailureReason = existingPayment.FailureReason
            };
        }

        // 2. Create payment record
        var payment = Payment.Create(
            tenantId: request.TenantId,
            amount: request.Amount,
            currency: "USD",
            idempotencyKey: idempotencyKey,
            provider: paymentGateway.ProviderId,
            subscriptionId: request.SubscriptionId,
            paymentMethodId: request.PaymentMethodId);

        await paymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);

        // 3. Mark as processing
        payment.MarkAsProcessing();
        await paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);

        // 4. Process through payment gateway
        var gatewayRequest = new GatewayPaymentRequest(
            IdempotencyKey: idempotencyKey,
            Amount: request.Amount,
            Currency: "USD",
            CustomerId: null,
            PaymentMethodId: request.PaymentMethodId,
            Description: $"Payment for subscription {request.SubscriptionId}",
            Metadata: new Dictionary<string, string>
            {
                ["tenant_id"] = request.TenantId.ToString(),
                ["subscription_id"] = request.SubscriptionId.ToString()
            });

        var gatewayResult = await paymentGateway.ProcessPaymentAsync(gatewayRequest, cancellationToken)
            .ConfigureAwait(false);

        // 5. Update payment based on gateway result
        if (gatewayResult.Success)
        {
            payment.MarkAsSucceeded(
                gatewayResult.ExternalPaymentId ?? gatewayResult.TransactionId ?? Guid.NewGuid().ToString(),
                gatewayResult.TransactionId);

            logger.LogInformation("Payment {PaymentId} succeeded with transaction {TransactionId}",
                payment.Id, gatewayResult.TransactionId);
        }
        else if (gatewayResult.Status == PaymentStatus.RequiresAction)
        {
            payment.MarkAsRequiresAction(gatewayResult.TransactionId);
            logger.LogInformation("Payment {PaymentId} requires additional action", payment.Id);
        }
        else
        {
            payment.MarkAsFailed(
                gatewayResult.ErrorMessage ?? "Payment processing failed",
                gatewayResult.ErrorCode);

            logger.LogWarning("Payment {PaymentId} failed: {ErrorMessage} ({ErrorCode})",
                payment.Id, gatewayResult.ErrorMessage, gatewayResult.ErrorCode);
        }

        await paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);

        if (gatewayResult.Success && payment.ProcessedAt.HasValue)
        {
            await paymentSubscriptionSyncService.SyncSuccessfulPaymentAsync(
                payment.Id.ToString(),
                payment.SubscriptionId,
                payment.Amount,
                payment.Currency,
                payment.ProcessedAt.Value,
                cancellationToken).ConfigureAwait(false);
        }

        // 6. Return result
        return new PaymentResult
        {
            Success = gatewayResult.Success,
            TransactionId = gatewayResult.TransactionId,
            PaymentId = payment.Id.ToString(),
            Amount = new Money(payment.Amount, payment.Currency),
            ProcessedAt = payment.ProcessedAt,
            Status = payment.Status,
            FailureReason = gatewayResult.ErrorMessage,
            PaymentMethodId = request.PaymentMethodId
        };
    }
}

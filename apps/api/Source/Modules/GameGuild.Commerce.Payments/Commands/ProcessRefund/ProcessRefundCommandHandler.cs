using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for processing refund commands
/// </summary>
public sealed class ProcessRefundCommandHandler(
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    ILogger<ProcessRefundCommandHandler> logger) : ICommandHandler<ProcessRefundCommand, ProcessRefundResult>
{
    public async Task<ProcessRefundResult> Handle(ProcessRefundCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing refund for payment {PaymentId}, amount {Amount}, reason: {Reason}",
            request.PaymentId, request.Amount, request.Reason);

        // 1. Get the payment
        var payment = await paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken)
            .ConfigureAwait(false);

        if (payment == null)
        {
            logger.LogWarning("Payment {PaymentId} not found for refund", request.PaymentId);
            return new ProcessRefundResult
            {
                RefundId = Guid.Empty,
                PaymentId = request.PaymentId,
                RefundedAmount = 0,
                Currency = "USD",
                Status = TransactionStatus.Failed,
                Reason = request.Reason,
                ProcessedAt = DateTime.UtcNow,
                IsSuccess = false,
                ErrorMessage = $"Payment {request.PaymentId} not found"
            };
        }

        // 2. Validate refund is possible
        if (payment.Status != PaymentStatus.Succeeded && payment.Status != PaymentStatus.Disputed)
        {
            logger.LogWarning("Payment {PaymentId} in status {Status} cannot be refunded",
                request.PaymentId, payment.Status);

            return new ProcessRefundResult
            {
                RefundId = Guid.Empty,
                PaymentId = request.PaymentId,
                RefundedAmount = 0,
                Currency = payment.Currency,
                Status = TransactionStatus.Failed,
                Reason = request.Reason,
                ProcessedAt = DateTime.UtcNow,
                IsSuccess = false,
                ErrorMessage = $"Payment in status {payment.Status} cannot be refunded"
            };
        }

        // 3. Validate refund amount
        var maxRefundable = payment.Amount - payment.RefundedAmount;
        if (request.Amount > maxRefundable)
        {
            logger.LogWarning("Refund amount {Amount} exceeds maximum refundable {MaxRefundable} for payment {PaymentId}",
                request.Amount, maxRefundable, request.PaymentId);

            return new ProcessRefundResult
            {
                RefundId = Guid.Empty,
                PaymentId = request.PaymentId,
                RefundedAmount = 0,
                Currency = payment.Currency,
                Status = TransactionStatus.Failed,
                Reason = request.Reason,
                ProcessedAt = DateTime.UtcNow,
                IsSuccess = false,
                ErrorMessage = $"Refund amount {request.Amount} exceeds maximum refundable {maxRefundable}"
            };
        }

        // 4. Process refund through payment gateway
        var refundIdempotencyKey = $"refund_{payment.Id}_{request.Amount}_{DateTime.UtcNow:yyyyMMddHHmmss}";
        var gatewayRequest = new GatewayRefundRequest(
            IdempotencyKey: refundIdempotencyKey,
            OriginalTransactionId: payment.ExternalTransactionId ?? payment.ExternalPaymentId ?? payment.Id.ToString(),
            Amount: request.Amount,
            Reason: request.Reason);

        var gatewayResult = await paymentGateway.ProcessRefundAsync(gatewayRequest, cancellationToken)
            .ConfigureAwait(false);

        if (!gatewayResult.Success)
        {
            logger.LogWarning("Refund failed for payment {PaymentId}: {ErrorMessage}",
                request.PaymentId, gatewayResult.ErrorMessage);

            return new ProcessRefundResult
            {
                RefundId = Guid.Empty,
                PaymentId = request.PaymentId,
                RefundedAmount = 0,
                Currency = payment.Currency,
                Status = TransactionStatus.Failed,
                Reason = request.Reason,
                ProcessedAt = DateTime.UtcNow,
                IsSuccess = false,
                ErrorMessage = gatewayResult.ErrorMessage
            };
        }

        // 5. Update payment with refund details
        var refundId = gatewayResult.RefundId ?? Guid.NewGuid().ToString();
        payment.ProcessRefund(request.Amount, refundId, request.Reason);
        await paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Refund {RefundId} processed successfully for payment {PaymentId}",
            refundId, request.PaymentId);

        // 6. Return result
        return new ProcessRefundResult
        {
            RefundId = Guid.TryParse(refundId, out var parsedId) ? parsedId : Guid.NewGuid(),
            PaymentId = request.PaymentId,
            RefundedAmount = request.Amount,
            Currency = payment.Currency,
            Status = TransactionStatus.Completed,
            Reason = request.Reason,
            ProcessedAt = DateTime.UtcNow,
            ReferenceNumber = refundId,
            EstimatedCompletionDate = DateTime.UtcNow.AddDays(5), // Standard refund processing time
            ProcessingFee = 0,
            IsSuccess = true
        };
    }
}

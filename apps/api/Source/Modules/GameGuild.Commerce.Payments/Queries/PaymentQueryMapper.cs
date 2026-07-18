using System.Globalization;
using System.Text.Json;

namespace GameGuild.Commerce.Payments;

internal static class PaymentQueryMapper
{
    public static PaymentResult ToResult(Payment payment)
    {
        return new PaymentResult
        {
            TenantId = payment.TenantId,
            Success = payment.Status == PaymentStatus.Succeeded,
            TransactionId = payment.ExternalTransactionId,
            PaymentId = payment.ExternalPaymentId ?? payment.Id.ToString("D", CultureInfo.InvariantCulture),
            Amount = new Money(Math.Max(payment.NetAmount, 0m), payment.Currency),
            ProcessedAt = payment.ProcessedAt ?? payment.RefundedAt ?? payment.CancelledAt ?? payment.UpdatedAt,
            FailureReason = payment.FailureReason ?? (payment.Status == PaymentStatus.Cancelled ? payment.CancellationReason : null),
            PaymentMethodId = payment.PaymentMethodId,
            Status = payment.Status,
            InvoiceId = payment.InvoiceId
        };
    }

    public static PaymentHistoryResult ToHistoryResult(Payment payment)
    {
        return new PaymentHistoryResult
        {
            PaymentId = payment.Id,
            UserId = TryGetUserId(payment.Metadata) ?? Guid.Empty,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status,
            PaymentMethod = payment.PaymentMethodId ?? payment.Provider,
            Description = payment.Description ?? string.Empty,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt,
            TransactionReference = payment.ExternalTransactionId ?? payment.ExternalPaymentId ?? payment.IdempotencyKey,
            MerchantName = payment.Provider,
            RefundedAmount = payment.RefundedAmount,
            ProcessingFee = 0m
        };
    }

    public static Guid? TryGetUserId(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadata);
            var root = document.RootElement;

            return TryReadGuid(root, "userId")
                ?? TryReadGuid(root, "UserId")
                ?? TryReadGuid(root, "customerUserId")
                ?? TryReadGuid(root, "CustomerUserId");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Guid? TryReadGuid(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && Guid.TryParse(property.GetString(), out var value)
                ? value
                : null;
    }
}

using System.Globalization;

namespace GameGuild.Commerce.Payments;

internal static class SubscriptionPaymentIdentity
{
    public static string CreateIdempotencyKey(Guid tenantId, Guid subscriptionId, int billingCycleNumber)
    {
        if (billingCycleNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(billingCycleNumber), "Billing cycle number must be positive.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"subscription:{tenantId:N}:{subscriptionId:N}:cycle:{billingCycleNumber}:charge");
    }

    public static int? TryGetBillingCycleNumber(string idempotencyKey)
    {
        var parts = idempotencyKey.Split(':', StringSplitOptions.TrimEntries);

        return parts.Length == 6
               && string.Equals(parts[0], "subscription", StringComparison.Ordinal)
               && Guid.TryParseExact(parts[1], "N", out _)
               && Guid.TryParseExact(parts[2], "N", out _)
               && string.Equals(parts[3], "cycle", StringComparison.Ordinal)
               && int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out var cycle)
               && cycle > 0
               && string.Equals(parts[5], "charge", StringComparison.Ordinal)
            ? cycle
            : null;
    }
}

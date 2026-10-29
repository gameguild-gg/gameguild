namespace GameGuild.Commerce.Payments;

internal static class GatewayPaymentResultBinder
{
    /// <summary>
    ///     Stripe Processing is a mapped provider state in the database. Until a real
    ///     mapping is available, Pending is the durable local claim: it has no retry
    ///     schedule and a repeated subscription charge returns the existing payment.
    /// </summary>
    public static void PrepareProcessingClaim(Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
        if (payment.Status != PaymentStatus.Pending) return;
        if (string.Equals(payment.Provider, GatewayProviderIds.Stripe, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(payment.ProviderEnvironment)
                || string.IsNullOrWhiteSpace(payment.ProviderAccountId)
                || string.IsNullOrWhiteSpace(payment.ProviderObjectId)
                || string.IsNullOrWhiteSpace(payment.ProviderObjectType)
                || string.IsNullOrWhiteSpace(payment.ProviderMonetaryLeg))) return;

        payment.MarkAsProcessing();
    }

    public static void BindVerifiedProviderMapping(
        Payment payment,
        string provider,
        GatewayPaymentResult gatewayResult)
    {
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(gatewayResult);

        if (gatewayResult.ProviderMapping is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            payment.BindProviderMapping(
                provider,
                gatewayResult.ProviderMapping.ProviderEnvironment,
                gatewayResult.ProviderMapping.ProviderAccountId,
                gatewayResult.ProviderMapping.ProviderObjectId,
                gatewayResult.ProviderMapping.ProviderObjectType,
                gatewayResult.ProviderMapping.ProviderMonetaryLeg);
            return;
        }

        var mappingIsRequired =
            gatewayResult.Success ||
            gatewayResult.Status is PaymentStatus.Succeeded or PaymentStatus.RequiresAction ||
            (gatewayResult.Status is PaymentStatus.Pending or PaymentStatus.Processing &&
             !string.IsNullOrWhiteSpace(gatewayResult.TransactionId));

        if (mappingIsRequired)
        {
            throw new InvalidOperationException(
                "A provider mapping is required before accepting this gateway payment state.");
        }
    }
}

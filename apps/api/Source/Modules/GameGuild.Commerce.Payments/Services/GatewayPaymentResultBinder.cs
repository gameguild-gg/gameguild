namespace GameGuild.Commerce.Payments;

internal static class GatewayPaymentResultBinder
{
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
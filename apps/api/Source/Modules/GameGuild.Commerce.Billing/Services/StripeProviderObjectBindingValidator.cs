using GameGuild.Commerce.Payments;

namespace GameGuild.Commerce.Billing;

public interface IStripeProviderObjectBindingValidator
{
    Task<StripeWebhookPaymentBinding?> ValidateAsync(
        VerifiedStripeWebhookEvent verifiedEvent,
        CancellationToken cancellationToken = default);
}

public sealed class StripeProviderObjectBindingValidator(IPaymentRepository paymentRepository)
    : IStripeProviderObjectBindingValidator
{
    public async Task<StripeWebhookPaymentBinding?> ValidateAsync(
        VerifiedStripeWebhookEvent verifiedEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verifiedEvent);

        if (verifiedEvent.ProviderMonetaryLeg is "nonmonetary" or "subscription")
        {
            return null;
        }

        var payment = await paymentRepository.GetByProviderMappingAsync(
                PaymentProviders.Stripe,
                verifiedEvent.ProviderEnvironment,
                verifiedEvent.ProviderAccountId,
                verifiedEvent.ProviderObjectId,
                verifiedEvent.ProviderObjectType,
                verifiedEvent.ProviderMonetaryLeg,
                cancellationToken)
            .ConfigureAwait(false);
        if (payment is null)
        {
            throw new InvalidWebhookPayloadException("Stripe event references an unknown payment provider object.");
        }

        if (verifiedEvent.TenantId.HasValue && verifiedEvent.TenantId.Value != payment.TenantId)
        {
            throw new InvalidWebhookPayloadException("Stripe event tenant does not match the payment owner.");
        }

        if (!verifiedEvent.Amount.HasValue || verifiedEvent.Amount.Value < 0)
        {
            throw new InvalidWebhookPayloadException("Stripe event amount is required for a monetary event.");
        }

        if (string.IsNullOrWhiteSpace(verifiedEvent.Currency) ||
            !string.Equals(verifiedEvent.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidWebhookPayloadException("Stripe event currency does not match the authoritative payment currency.");
        }

        if (verifiedEvent.ProviderMonetaryLeg is "capture" or "failure")
        {
            if (verifiedEvent.Amount.Value != payment.Amount)
            {
                throw new InvalidWebhookPayloadException("Stripe event amount does not match the authoritative payment amount.");
            }
        }
        else if (verifiedEvent.Amount.Value > payment.Amount)
        {
            throw new InvalidWebhookPayloadException("Stripe event amount exceeds the authoritative payment amount.");
        }

        if (verifiedEvent.CumulativeRefundedAmount.HasValue &&
            verifiedEvent.CumulativeRefundedAmount.Value < payment.RefundedAmount)
        {
            throw new InvalidWebhookPayloadException("Stripe cumulative refund total regressed below the locally confirmed refund total.");
        }

        try
        {
            payment.ValidateProviderMonetaryBounds(
                payment.Amount,
                verifiedEvent.CumulativeRefundedAmount ?? payment.RefundedAmount,
                verifiedEvent.CumulativeDisputedAmount ?? 0m);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentOutOfRangeException)
        {
            throw new InvalidWebhookPayloadException("Stripe cumulative provider amounts violate the authoritative payment bounds.", exception);
        }

        return new StripeWebhookPaymentBinding(payment.Id, payment.TenantId);
    }
}

public sealed record StripeWebhookPaymentBinding(Guid PaymentId, Guid TenantId);

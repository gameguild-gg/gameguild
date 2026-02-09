using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for recording subscription payments with out-of-order protection.
///     Does not use base handler due to custom return type (PaymentRecordResult).
/// </summary>
public sealed class RecordSubscriptionPaymentCommandHandler(ISubscriptionRepository subscriptionRepository) 
    : ICommandHandler<RecordSubscriptionPaymentCommand, PaymentRecordResult>
{
    public async Task<PaymentRecordResult> Handle(RecordSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken)
            .ConfigureAwait(false);

        if (subscription is null) 
            throw new SubscriptionNotFoundException(request.SubscriptionId);

        // Record the payment with idempotency key and optional billing cycle
        var result = subscription.RecordPayment(
            request.Amount, 
            request.Currency, 
            request.PaymentDate, 
            request.IdempotencyKey,
            request.ForBillingCycle);

        // Save changes only if payment was successfully recorded
        if (result.IsSuccess)
        {
            await subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}

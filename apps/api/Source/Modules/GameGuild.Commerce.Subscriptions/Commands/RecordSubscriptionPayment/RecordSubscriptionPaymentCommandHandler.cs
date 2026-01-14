using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for recording subscription payments
/// </summary>
public class RecordSubscriptionPaymentCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<RecordSubscriptionPaymentCommand>
{
    public async Task<Unit> Handle(RecordSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        if (subscription is null) throw new InvalidOperationException("Subscription not found");

        // Record the payment with idempotency key
        subscription.RecordPayment(request.Amount, request.Currency, request.PaymentDate, request.IdempotencyKey);

        // Save changes
        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}

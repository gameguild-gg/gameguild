using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Handler for recording subscription payments
/// </summary>
public class RecordSubscriptionPaymentCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<RecordSubscriptionPaymentCommand>
{
    public async Task<Unit> Handle(RecordSubscriptionPaymentCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        if (subscription is null) throw new InvalidOperationException("Subscription not found");

        // Record the payment
        subscription.RecordPayment(request.Amount, request.Currency, request.PaymentDate);

        // Save changes
        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}

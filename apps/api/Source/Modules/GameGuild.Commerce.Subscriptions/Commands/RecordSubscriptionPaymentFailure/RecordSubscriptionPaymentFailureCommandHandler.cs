using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for recording subscription payment failures
/// </summary>
public class RecordSubscriptionPaymentFailureCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<RecordSubscriptionPaymentFailureCommand>
{
    public async Task<Unit> Handle(RecordSubscriptionPaymentFailureCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        if (subscription is null) throw new InvalidOperationException("Subscription not found");

        // Record the payment failure
        subscription.RecordPaymentFailure(request.Reason, request.FailureDate);

        // Save changes
        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}

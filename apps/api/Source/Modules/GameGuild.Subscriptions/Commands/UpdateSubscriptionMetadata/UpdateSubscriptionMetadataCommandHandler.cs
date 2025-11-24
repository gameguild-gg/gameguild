using GameGuild.CQRS;
using GameGuild.Subscriptions.Abstractions;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Commands;

/// <summary>
///     Handler for updating subscription metadata
/// </summary>
public class UpdateSubscriptionMetadataCommandHandler(ISubscriptionRepository subscriptionRepository) : ICommandHandler<UpdateSubscriptionMetadataCommand>
{
    public async Task<Unit> Handle(UpdateSubscriptionMetadataCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);

        if (subscription is null) throw new InvalidOperationException("Subscription not found");

        // Update the metadata
        subscription.UpdateMetadata(request.Metadata);

        // Save changes
        await subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        return Unit.Value;
    }
}

using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for SetSubscriptionExternalIdsCommand.
///     Delegates to the external ID service to persist provider identifiers.
/// </summary>
public sealed class SetSubscriptionExternalIdsCommandHandler(ISubscriptionExternalIdService externalIdService)
    : ICommandHandler<SetSubscriptionExternalIdsCommand>
{
    public async Task<Unit> Handle(SetSubscriptionExternalIdsCommand request, CancellationToken cancellationToken)
    {
        await externalIdService
            .SetExternalIdsAsync(request.SubscriptionId, request.StripeSubscriptionId, request.PayPalSubscriptionId, cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
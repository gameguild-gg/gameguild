using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Handler for ReactivateSubscriptionCommand.
///     Delegates to the lifecycle service for subscription reactivation.
/// </summary>
/// <remarks>
///     Issue #409 remainder: reactivating a cancelled subscription is the resubscribe
///     path — the moment billing resumes without a fresh checkout — so the attempt runs
///     the fraud signal evaluation through <see cref="SubscriptionChangeFraudGate"/>
///     BEFORE the lifecycle mutation: Block refuses the reactivation (422), Review
///     persists the bounded evidence on the subscription so reviewers see it before the
///     resumed charge clears the payment gate.
/// </remarks>
public sealed class ReactivateSubscriptionCommandHandler(
    ISubscriptionLifecycleService lifecycleService,
    ISubscriptionRepository subscriptionRepository,
    ILogger<ReactivateSubscriptionCommandHandler>? logger = null,
    GameGuild.Commerce.Payments.IFraudDetectionService? fraudDetectionService = null,
    GameGuild.Commerce.IFraudReviewTicketCoordinator? reviewTicketCoordinator = null)
    : ICommandHandler<ReactivateSubscriptionCommand>
{
    public async Task<Unit> Handle(ReactivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (fraudDetectionService is not null)
        {
            var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false)
                ?? throw new GameGuild.EntityNotFoundException("Subscription", request.SubscriptionId);

            await SubscriptionChangeFraudGate.AssessAsync(
                fraudDetectionService,
                logger ?? NullLogger<ReactivateSubscriptionCommandHandler>.Instance,
                subscriptionRepository,
                subscription,
                GameGuild.Commerce.Payments.SubscriptionFraudChangeKind.Reactivation,
                subscription.Amount.Amount,
                subscription.Amount.Currency,
                request.FraudCountry,
                request.FraudSourceIpAddress,
                request.FraudDeviceFingerprint,
                request.FraudPayerEmail,
                request.FraudCardBin,
                request.FraudClientTimezone,
                reviewTicketCoordinator,
                cancellationToken).ConfigureAwait(false);
        }

        await lifecycleService.ReactivateAsync(request.SubscriptionId, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

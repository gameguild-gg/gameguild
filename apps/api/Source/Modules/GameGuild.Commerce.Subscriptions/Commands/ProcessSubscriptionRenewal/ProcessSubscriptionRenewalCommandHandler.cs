using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Command handler for processing a subscription renewal.
///     Delegates to the billing service which handles idempotency, plan lookup, and payment.
/// </summary>
public sealed class ProcessSubscriptionRenewalCommandHandler(ISubscriptionBillingService billingService)
    : ICommandHandler<ProcessSubscriptionRenewalCommand>
{
    /// <inheritdoc />
    public async Task<Unit> Handle(ProcessSubscriptionRenewalCommand request, CancellationToken cancellationToken)
    {
        var result = await billingService.ProcessRenewalAsync(request.SubscriptionId, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            throw new RequestValidationException([
                new ValidationError(
                    nameof(ProcessSubscriptionRenewalCommand.SubscriptionId),
                    result.FailureReason ?? $"Failed to renew subscription {request.SubscriptionId}",
                    request.SubscriptionId)
            ]);
        }

        return Unit.Value;
    }
}

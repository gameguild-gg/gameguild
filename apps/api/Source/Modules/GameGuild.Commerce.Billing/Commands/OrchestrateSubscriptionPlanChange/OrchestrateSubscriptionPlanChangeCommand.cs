using FluentValidation;
using GameGuild.CQRS;
using GameGuild.Commerce.Subscriptions;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Request body for <see cref="OrchestrateSubscriptionPlanChangeCommand" /> (issue #396
///     workflow orchestration). The acting user is never part of the body: the handler
///     resolves it from the ambient actor context.
/// </summary>
public sealed record OrchestrateSubscriptionPlanChangeRequest(
    Guid? NewPlanId = null,
    DateTime? EffectiveDateUtc = null,
    Guid? PaymentMethodId = null);

/// <summary>
///     Runs the orchestrated subscription plan change saga (issue #396 workflow
///     orchestration): proration, invoice, payment and activation as journaled,
///     event-observable steps with compensating actions on failure.
/// </summary>
public sealed record OrchestrateSubscriptionPlanChangeCommand(
    Guid SubscriptionId,
    Guid NewPlanId,
    DateTime? EffectiveDateUtc = null,
    Guid? PaymentMethodId = null) : ICommand<SubscriptionChangeSagaResult>;

/// <summary>Validator for <see cref="OrchestrateSubscriptionPlanChangeCommand" /> (fail-closed).</summary>
public sealed class OrchestrateSubscriptionPlanChangeCommandValidator : AbstractValidator<OrchestrateSubscriptionPlanChangeCommand>
{
    public OrchestrateSubscriptionPlanChangeCommandValidator()
    {
        RuleFor(command => command.SubscriptionId).NotEmpty();
        RuleFor(command => command.NewPlanId).NotEmpty();
        RuleFor(command => command.NewPlanId)
            .NotEqual(command => command.SubscriptionId)
            .WithMessage("NewPlanId must differ from SubscriptionId.");
        RuleFor(command => command.EffectiveDateUtc)
            .Must(effectiveDate => !effectiveDate.HasValue || effectiveDate.Value <= SystemClock.UtcNow.AddMinutes(5))
            .WithMessage("EffectiveDateUtc cannot be in the future beyond a five-minute clock-skew tolerance.");
    }
}

/// <summary>
///     Handler for <see cref="OrchestrateSubscriptionPlanChangeCommand" />: resolves the
///     acting user from the ambient actor context (never from the request) and delegates
///     to the saga coordinator.
/// </summary>
public sealed class OrchestrateSubscriptionPlanChangeCommandHandler(
    ISubscriptionChangeSaga saga,
    IActorContextAccessor actorContextAccessor,
    ISubscriptionRepository subscriptions) : ICommandHandler<OrchestrateSubscriptionPlanChangeCommand, SubscriptionChangeSagaResult>
{
    public async Task<SubscriptionChangeSagaResult> Handle(
        OrchestrateSubscriptionPlanChangeCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.SubjectIdAsGuid is not { } actorId || actorId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("An authenticated user is required to change a subscription plan.");
        }

        var subscription = await subscriptions.GetByIdAsync(command.SubscriptionId, cancellationToken)
            .ConfigureAwait(false) ?? throw new SubscriptionNotFoundException(command.SubscriptionId);
        if (!actor.IsSystemAdmin && (actor.TenantId != subscription.TenantId
            || (subscription.CreatedByUserId != actorId && !actor.IsTenantAdmin)))
        {
            throw new UnauthorizedAccessException("Only the subscription owner or a tenant administrator can change this plan.");
        }

        return await saga
            .ExecuteAsync(
                new SubscriptionChangeSagaRequest(
                    command.SubscriptionId,
                    command.NewPlanId,
                    command.EffectiveDateUtc,
                    command.PaymentMethodId,
                    actorId),
                cancellationToken)
            .ConfigureAwait(false);
    }
}

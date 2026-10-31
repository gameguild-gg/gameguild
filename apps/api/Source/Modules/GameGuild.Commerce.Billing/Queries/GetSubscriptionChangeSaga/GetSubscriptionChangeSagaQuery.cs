using FluentValidation;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Read model returned by <see cref="GetSubscriptionChangeSagaQuery" /> (issue #396
///     workflow-orchestration observability): the durable saga row — status, current step,
///     proration amounts, payment reference, failure and compensation record — plus the
///     journaled step trail in execution order.
/// </summary>
public sealed record SubscriptionChangeSagaDto(
    Guid Id,
    Guid SubscriptionId,
    Guid TenantId,
    Guid FromPlanId,
    Guid ToPlanId,
    Guid InitiatedByActorId,
    string IdempotencyKey,
    string Status,
    string CurrentStep,
    decimal CreditForUnused,
    decimal ChargeForNew,
    decimal NetAdjustment,
    string Currency,
    DateTime ProrationEffectiveDateUtc,
    Guid? InvoiceId,
    string? PaymentTransactionId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    string? FailedStep,
    string? FailureReason,
    string? CompensationsApplied,
    IReadOnlyList<SubscriptionChangeSagaStepDto> Steps);

/// <summary>One journaled saga step attempt, in execution order.</summary>
public sealed record SubscriptionChangeSagaStepDto(
    int Sequence,
    string Step,
    string Outcome,
    string Detail,
    DateTime OccurredAtUtc);

/// <summary>
///     Query (issue #396 workflow orchestration): the most recent subscription plan-change
///     saga of one subscription, with its journaled steps and compensations — the recovery
///     and debugging surface for the orchestrated workflow.
/// </summary>
public sealed record GetSubscriptionChangeSagaQuery(Guid SubscriptionId)
    : IQuery<SubscriptionChangeSagaDto?>;

/// <summary>Validator for <see cref="GetSubscriptionChangeSagaQuery" />.</summary>
public sealed class GetSubscriptionChangeSagaQueryValidator : AbstractValidator<GetSubscriptionChangeSagaQuery>
{
    public GetSubscriptionChangeSagaQueryValidator()
    {
        RuleFor(query => query.SubscriptionId).NotEmpty();
    }
}

/// <summary>
///     Handler for <see cref="GetSubscriptionChangeSagaQuery" />: reads the latest durable
///     saga of the subscription together with its step journal. Returns <c>null</c> when the
///     subscription never went through an orchestrated plan change.
/// </summary>
public sealed class GetSubscriptionChangeSagaQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetSubscriptionChangeSagaQuery, SubscriptionChangeSagaDto?>
{
    /// <inheritdoc />
    public async Task<SubscriptionChangeSagaDto?> Handle(
        GetSubscriptionChangeSagaQuery request,
        CancellationToken cancellationToken)
    {
        var saga = await context.Set<SubscriptionChangeSaga>()
            .Include(entity => entity.Steps)
            .Where(entity => entity.SubscriptionId == request.SubscriptionId)
            .OrderByDescending(entity => entity.StartedAtUtc)
            .ThenByDescending(entity => entity.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (saga is null)
        {
            return null;
        }

        return new SubscriptionChangeSagaDto(
            saga.Id,
            saga.SubscriptionId,
            saga.TenantId ?? Guid.Empty,
            saga.FromPlanId,
            saga.ToPlanId,
            saga.InitiatedByActorId,
            saga.IdempotencyKey,
            saga.Status.ToString(),
            saga.CurrentStep,
            saga.CreditForUnused,
            saga.ChargeForNew,
            saga.NetAdjustment,
            saga.Currency,
            saga.ProrationEffectiveDateUtc,
            saga.InvoiceId,
            saga.PaymentTransactionId,
            saga.StartedAtUtc,
            saga.CompletedAtUtc,
            saga.FailedStep,
            saga.FailureReason,
            saga.CompensationsApplied,
            saga.Steps
                .OrderBy(step => step.Sequence)
                .Select(step => new SubscriptionChangeSagaStepDto(
                    step.Sequence,
                    step.Step,
                    step.Outcome,
                    step.Detail,
                    step.OccurredAtUtc))
                .ToArray());
    }
}

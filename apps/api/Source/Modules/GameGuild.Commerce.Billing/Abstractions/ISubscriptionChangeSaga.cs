namespace GameGuild.Commerce.Billing;

/// <summary>
///     Request to run the orchestrated subscription plan change saga (issue #396 workflow
///     orchestration). The acting user is resolved from the ambient actor context by the
///     command layer and passed here; it never travels in an HTTP body.
/// </summary>
/// <param name="SubscriptionId">Subscription aggregate to change.</param>
/// <param name="NewPlanId">Plan the subscription should land on.</param>
/// <param name="EffectiveDateUtc">Proration effective date; null means now.</param>
/// <param name="PaymentMethodId">
///     Optional local payment-method registry id to charge. Falls back to the subscription's
///     payment method; required when the proration yields a positive net due.
/// </param>
/// <param name="ActorId">Acting user from the ambient actor context.</param>
public sealed record SubscriptionChangeSagaRequest(
    Guid SubscriptionId,
    Guid NewPlanId,
    DateTime? EffectiveDateUtc,
    Guid? PaymentMethodId,
    Guid ActorId);

/// <summary>Outcome of one executed saga step, in execution order.</summary>
public sealed record SubscriptionChangeSagaStepResult(
    string Step,
    string Outcome,
    string Detail,
    DateTime OccurredAtUtc);

/// <summary>
///     Terminal result of the orchestrated subscription plan change: the saga state, the
///     executed steps and, on failure, the compensating actions that were applied.
/// </summary>
public sealed record SubscriptionChangeSagaResult(
    Guid SagaId,
    Guid SubscriptionId,
    SubscriptionChangeSagaStatus Status,
    Guid FromPlanId,
    Guid ToPlanId,
    decimal NetAdjustment,
    string Currency,
    Guid? InvoiceId,
    string? PaymentTransactionId,
    string? FailedStep,
    string? FailureReason,
    string? CompensationsApplied,
    IReadOnlyList<SubscriptionChangeSagaStepResult> Steps)
{
    /// <summary>The saga completed: the subscription is on the new plan.</summary>
    public bool Succeeded => Status == SubscriptionChangeSagaStatus.Completed;
}

/// <summary>
///     Saga coordinator for the subscription plan change workflow (issue #396 workflow
///     orchestration). Orchestrates the multi-step billing flow — proration, invoice,
///     payment, activation — as explicit, journaled steps with compensating actions
///     (refund, invoice void) when a later step fails, instead of leaving the flow to
///     independently reacting event handlers.
/// </summary>
/// <remarks>
///     <para>
///         Every step attempt appends a journal row and a durable step event atomically with
///         the saga state transition, so the workflow is observable end to end in the durable
///         billing event log and resumable by evidence. The external gateway charge cannot be
///         rolled back by a database transaction, which is exactly why the saga carries
///         compensating actions: a failed activation refunds the charge and voids the invoice.
///     </para>
///     <para>
///         Idempotency: sagas are keyed by the change intent (subscription, target plan,
///         effective date). A replayed request resolves to the already-recorded saga instead
///         of charging twice.
///     </para>
/// </remarks>
public interface ISubscriptionChangeSaga
{
    /// <summary>Executes (or resolves the already-recorded outcome of) one plan change saga.</summary>
    Task<SubscriptionChangeSagaResult> ExecuteAsync(
        SubscriptionChangeSagaRequest request,
        CancellationToken cancellationToken = default);
}

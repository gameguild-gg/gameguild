namespace GameGuild.Commerce.Billing;

/// <summary>
///     One saga step transition of the orchestrated subscription plan change (issue #396
///     workflow orchestration). Emitted for every step attempt (succeeded or not) so the
///     whole workflow is observable in the durable billing event log.
/// </summary>
public sealed record BillingSubscriptionChangeStepV1(
    [property: NonPersonalEventData] Guid SagaId,
    [property: NonPersonalEventData] Guid SubscriptionId,
    [property: NonPersonalEventData] string Step,
    [property: NonPersonalEventData] string Outcome,
    [property: NonPersonalEventData] string Detail) : DurableIntegrationEventBase
{
    public override string EventName => "commerce.billing.subscription-change-step.v1";
    public override string SourceModule => "Commerce.Billing";
}

/// <summary>
///     An orchestrated subscription plan change completed end to end: proration calculated,
///     invoice settled and the subscription activated on the new plan (issue #396 workflow
///     orchestration). This is the event a state projection folds to move
///     <c>CurrentPlanId</c> forward.
/// </summary>
public sealed record BillingSubscriptionChangeCompletedV1(
    [property: NonPersonalEventData] Guid SagaId,
    [property: NonPersonalEventData] Guid SubscriptionId,
    [property: NonPersonalEventData] Guid FromPlanId,
    [property: NonPersonalEventData] Guid ToPlanId,
    [property: NonPersonalEventData] Guid InvoiceId,
    [property: NonPersonalEventData] string PaymentTransactionId,
    [property: NonPersonalEventData] decimal NetAdjustment,
    [property: NonPersonalEventData] string Currency) : DurableIntegrationEventBase
{
    public override string EventName => "commerce.billing.subscription-change-completed.v1";
    public override string SourceModule => "Commerce.Billing";
}

/// <summary>
///     An orchestrated subscription plan change failed after <c>FailedStep</c> and was
///     rolled back through the listed compensating actions (issue #396 workflow
///     orchestration). The subscription keeps its original plan when compensation succeeds.
/// </summary>
public sealed record BillingSubscriptionChangeFailedV1(
    [property: NonPersonalEventData] Guid SagaId,
    [property: NonPersonalEventData] Guid SubscriptionId,
    [property: NonPersonalEventData] Guid FromPlanId,
    [property: NonPersonalEventData] Guid ToPlanId,
    [property: NonPersonalEventData] string FailedStep,
    [property: NonPersonalEventData] string Reason,
    [property: NonPersonalEventData] string CompensationsApplied) : DurableIntegrationEventBase
{
    public override string EventName => "commerce.billing.subscription-change-failed.v1";
    public override string SourceModule => "Commerce.Billing";
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     Lifecycle status of an orchestrated subscription plan change saga (issue #396
///     workflow orchestration). The saga moves <see cref="Running" /> through the steps
///     (proration, invoice, payment, activation) and lands in <see cref="Completed" /> or,
///     after compensating actions, <see cref="Failed" />.
/// </summary>
public enum SubscriptionChangeSagaStatus
{
    /// <summary>The saga row was created and is about to start its first step.</summary>
    Pending = 0,

    /// <summary>At least one step has executed and the workflow is still progressing.</summary>
    Running = 1,

    /// <summary>All steps succeeded; the subscription is on the new plan.</summary>
    Completed = 2,

    /// <summary>A step failed and compensating actions are being applied.</summary>
    Compensating = 3,

    /// <summary>The saga finished in a rolled-back or abandoned state; the subscription
    /// keeps its original plan whenever compensation succeeded.</summary>
    Failed = 4
}

/// <summary>
///     Durable state of one orchestrated subscription plan change (issue #396 workflow
///     orchestration). Each executed step appends a <see cref="SubscriptionChangeSagaStep" />
///     journal row, so a saga is fully observable and resumable-by-evidence after the fact:
///     the journal plus the durable billing events reconstruct exactly what happened.
/// </summary>
[Table("subscription_change_sagas")]
public class SubscriptionChangeSaga : EntityBase
{
    private SubscriptionChangeSaga()
    {
    }

    /// <summary>
    ///     Creates saga state for one subscription plan change intent. The idempotency key
    ///     (subscription + target plan + effective date) has a unique index so a replayed
    ///     request resumes the original saga instead of double-charging.
    /// </summary>
    public SubscriptionChangeSaga(
        Guid tenantId,
        Guid subscriptionId,
        Guid fromPlanId,
        Guid toPlanId,
        Guid initiatedByActorId,
        string idempotencyKey)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required for financial entities (fail-closed)", nameof(tenantId));
        }

        TenantId = tenantId;
        SubscriptionId = subscriptionId;
        FromPlanId = fromPlanId;
        ToPlanId = toPlanId;
        InitiatedByActorId = initiatedByActorId;
        IdempotencyKey = idempotencyKey;
        Status = SubscriptionChangeSagaStatus.Pending;
    }

    /// <summary>Subscription aggregate the change targets.</summary>
    [Required]
    public Guid SubscriptionId { get; private set; }

    /// <summary>Plan the subscription is on when the saga starts.</summary>
    [Required]
    public Guid FromPlanId { get; private set; }

    /// <summary>Plan the subscription should land on when the saga completes.</summary>
    [Required]
    public Guid ToPlanId { get; private set; }

    /// <summary>Actor that initiated the change, from the ambient actor context (never the request body).</summary>
    public Guid InitiatedByActorId { get; private set; }

    /// <summary>
    ///     Unique key of the change intent (<c>subscription:{id}:change:{plan}:{effectiveDate}</c>);
    ///     replays of the same intent resolve to the same saga row.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>Current lifecycle status of the saga.</summary>
    public SubscriptionChangeSagaStatus Status { get; private set; }

    /// <summary>Step the saga is currently executing or failed on.</summary>
    [MaxLength(50)]
    public string CurrentStep { get; private set; } = "Proration";

    /// <summary>Credit for the unused remainder of the old plan, from the proration step.</summary>
    public decimal CreditForUnused { get; private set; }

    /// <summary>Charge for the new plan over the remaining period, from the proration step.</summary>
    public decimal ChargeForNew { get; private set; }

    /// <summary>Net amount due for the change (charge minus credit).</summary>
    public decimal NetAdjustment { get; private set; }

    /// <summary>Currency of the proration amounts (ISO 4217).</summary>
    [MaxLength(3)]
    public string Currency { get; private set; } = "USD";

    /// <summary>Date the proration was computed against.</summary>
    public DateTime ProrationEffectiveDateUtc { get; private set; }

    /// <summary>Invoice issued by the invoice step, when the change has a positive net due.</summary>
    public Guid? InvoiceId { get; private set; }

    /// <summary>Gateway transaction id of the payment step charge.</summary>
    [MaxLength(255)]
    public string? PaymentTransactionId { get; private set; }

    /// <summary>When the saga started executing.</summary>
    public DateTime StartedAtUtc { get; private set; }

    /// <summary>When the saga reached a terminal status (completed or failed).</summary>
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>Step that failed, when the saga failed.</summary>
    [MaxLength(50)]
    public string? FailedStep { get; private set; }

    /// <summary>Why the saga failed (technical processing detail, never credentials).</summary>
    [MaxLength(1000)]
    public string? FailureReason { get; private set; }

    /// <summary>Compensating actions applied before the saga failed, in application order.</summary>
    [MaxLength(1000)]
    public string? CompensationsApplied { get; private set; }

    /// <summary>Step journal rows, oldest first.</summary>
    public virtual List<SubscriptionChangeSagaStep> Steps { get; private set; } = [];

    /// <summary>Marks the saga as running from its first step.</summary>
    public void Start(string firstStep, DateTime startedAtUtc)
    {
        Status = SubscriptionChangeSagaStatus.Running;
        CurrentStep = firstStep;
        StartedAtUtc = startedAtUtc;
    }

    /// <summary>Records the proration outcome of the proration step.</summary>
    public void RecordProration(
        decimal creditForUnused,
        decimal chargeForNew,
        decimal netAdjustment,
        string currency,
        DateTime effectiveDateUtc)
    {
        CreditForUnused = creditForUnused;
        ChargeForNew = chargeForNew;
        NetAdjustment = netAdjustment;
        Currency = currency;
        ProrationEffectiveDateUtc = effectiveDateUtc;
    }

    /// <summary>Advances the saga to the next step.</summary>
    public void AdvanceTo(string step)
    {
        CurrentStep = step;
    }

    /// <summary>Records the invoice issued by the invoice step.</summary>
    public void RecordInvoice(Guid invoiceId)
    {
        InvoiceId = invoiceId;
    }

    /// <summary>Records the gateway charge of the payment step.</summary>
    public void RecordPayment(string transactionId)
    {
        PaymentTransactionId = transactionId;
    }

    /// <summary>Marks the saga completed on the new plan.</summary>
    public void Complete(DateTime completedAtUtc)
    {
        Status = SubscriptionChangeSagaStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>Marks the saga as executing compensating actions after a failed step.</summary>
    public void BeginCompensation(string failedStep, string reason, DateTime failedAtUtc)
    {
        Status = SubscriptionChangeSagaStatus.Compensating;
        FailedStep = failedStep;
        FailureReason = reason.Length > 1000 ? reason[..1000] : reason;
        CompletedAtUtc = failedAtUtc;
    }

    /// <summary>Marks the saga failed after compensating actions were applied.</summary>
    public void Fail(string compensationsApplied)
    {
        Status = SubscriptionChangeSagaStatus.Failed;
        CompensationsApplied = compensationsApplied;
    }

    /// <summary>Appends a journal row for an executed step attempt.</summary>
    public void AppendStep(SubscriptionChangeSagaStep step) => Steps.Add(step);
}

/// <summary>
///     Journal row of one saga step attempt (issue #396 workflow orchestration): step name,
///     outcome and detail, in execution order. The journal is the observability record the
///     saga state and the durable step events are verified against.
/// </summary>
[Table("subscription_change_saga_steps")]
public class SubscriptionChangeSagaStep : EntityBase
{
    private SubscriptionChangeSagaStep()
    {
    }

    public SubscriptionChangeSagaStep(Guid sagaId, int sequence, string step, string outcome, string detail, DateTime occurredAtUtc)
    {
        SagaId = sagaId;
        Sequence = sequence;
        Step = step;
        Outcome = outcome;
        Detail = detail.Length > 500 ? detail[..500] : detail;
        OccurredAtUtc = occurredAtUtc;
    }

    /// <summary>Saga this journal row belongs to.</summary>
    [Required]
    public Guid SagaId { get; private set; }

    /// <summary>Execution order of the step attempt within the saga (1-based).</summary>
    public int Sequence { get; private set; }

    /// <summary>Step name (Proration, Invoice, Payment, Activation).</summary>
    [Required]
    [MaxLength(50)]
    public string Step { get; private set; } = string.Empty;

    /// <summary>Outcome of the attempt (Succeeded, Failed, Skipped, Compensated).</summary>
    [Required]
    [MaxLength(20)]
    public string Outcome { get; private set; } = string.Empty;

    /// <summary>Technical detail of the attempt (amounts, identifiers, errors).</summary>
    [MaxLength(500)]
    public string Detail { get; private set; } = string.Empty;

    /// <summary>When the step attempt executed.</summary>
    public DateTime OccurredAtUtc { get; private set; }
}

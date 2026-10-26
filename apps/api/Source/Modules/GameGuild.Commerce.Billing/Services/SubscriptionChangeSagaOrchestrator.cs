using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Billing;

/// <summary>
///     <see cref="ISubscriptionChangeSaga" /> implementation (issue #396 workflow
///     orchestration): coordinates the subscription plan change as an explicit multi-step
///     workflow — proration, invoice, payment, activation — with journaled steps, durable
///     step events and compensating actions for confirmed business failures. Local
///     state and required events share a transaction. An uncertain persistence or
///     transport outcome propagates for rollback and provider reconciliation; it must
///     never be converted to a confirmed decline or an automatic refund.
/// </summary>
public sealed class SubscriptionChangeSagaOrchestrator(
    IApplicationDbContext context,
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionPlanService planService,
    ISubscriptionIntervalPricingService intervalPricingService,
    IBillingCalculator billingCalculator,
    IPaymentGateway paymentGateway,
    IPaymentMethodRepository paymentMethodRepository,
    IBillingIntegrationEventPublisher billingEvents,
    ILogger<SubscriptionChangeSagaOrchestrator> logger,
    IGatewayPaymentMethodIdProtector gatewayIdProtector) : ISubscriptionChangeSaga
{
    public const string ProrationStep = "Proration";
    public const string InvoiceStep = "Invoice";
    public const string PaymentStep = "Payment";
    public const string ActivationStep = "Activation";

    /// <inheritdoc />
    public Task<SubscriptionChangeSagaResult> ExecuteAsync(
        SubscriptionChangeSagaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Commands already run inside the use-case transaction. Direct service calls
        // must provide the same atomic boundary for the plan, invoice and journal.
        if (context is DbContext database && database.Database.IsRelational()
            && database.Database.CurrentTransaction is null)
        {
            return database.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await context.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var result = await ExecuteCoreAsync(request, cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return result;
                }
                catch
                {
                    try
                    {
                        await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    finally
                    {
                        database.ChangeTracker.Clear();
                    }
                    throw;
                }
            });
        }

        return ExecuteCoreAsync(request, cancellationToken);
    }

    private async Task<SubscriptionChangeSagaResult> ExecuteCoreAsync(
        SubscriptionChangeSagaRequest request,
        CancellationToken cancellationToken)
    {

        var subscription = await subscriptionRepository
            .GetByIdAsync(request.SubscriptionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new SubscriptionNotFoundException(request.SubscriptionId);

        // Idempotency by change intent: a replayed request resolves to the recorded saga.
        var effectiveDate = request.EffectiveDateUtc ?? SystemClock.UtcNow;
        var idempotencyKey = BuildIdempotencyKey(request.SubscriptionId, request.NewPlanId, effectiveDate);
        var existing = await FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            logger.LogInformation(
                "Subscription change saga {SagaId} for subscription {SubscriptionId} already recorded in status {Status}; returning recorded outcome",
                existing.Id,
                request.SubscriptionId,
                existing.Status);
            return ToResult(existing);
        }

        if (subscription.PlanId == request.NewPlanId)
        {
            throw new BusinessRuleViolationException(
                "subscription-change.same-plan",
                $"Subscription {request.SubscriptionId} is already on plan {request.NewPlanId}.");
        }

        var saga = new SubscriptionChangeSaga(
            subscription.TenantId ?? throw new BusinessRuleViolationException(
                "subscription-change.tenant-required",
                $"Subscription {request.SubscriptionId} has no tenant; plan changes require a tenant-scoped subscription."),
            subscription.Id,
            subscription.PlanId,
            request.NewPlanId,
            request.ActorId,
            idempotencyKey);
        saga.Start(ProrationStep, SystemClock.UtcNow);
        context.Set<SubscriptionChangeSaga>().Add(saga);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // ── Step 1: Proration ───────────────────────────────────────────────────────
        var targetPlan = await planService
            .GetByIdAsync(request.NewPlanId, cancellationToken)
            .ConfigureAwait(false);
        if (targetPlan is null)
        {
            await CompensateAsync(
                saga,
                ProrationStep,
                $"Target plan {request.NewPlanId} not found.",
                invoice: null,
                transactionId: null,
                cancellationToken)
                .ConfigureAwait(false);
            return ToResult(saga);
        }

        var newAmount = await intervalPricingService
            .ResolvePriceForCycleAsync(targetPlan, subscription.BillingCycle, cancellationToken)
            .ConfigureAwait(false);
        var proration = billingCalculator.CalculateProration(
            subscription.Amount,
            newAmount,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            effectiveDate);
        saga.RecordProration(
            proration.CreditForUnused,
            proration.ChargeForNew,
            proration.NetAdjustment,
            subscription.Amount.Currency,
            proration.EffectiveDate);
        await RecordStepAsync(
            saga,
            ProrationStep,
            "Succeeded",
            $"credit={proration.CreditForUnused:F2} charge={proration.ChargeForNew:F2} net={proration.NetAdjustment:F2} {subscription.Amount.Currency}",
            cancellationToken)
            .ConfigureAwait(false);

        // ── Step 2: Invoice (skipped when the change is a net credit) ──────────────
        saga.AdvanceTo(InvoiceStep);
        Invoice? invoice = null;
        if (proration.NetAdjustment > 0)
        {
            invoice = new Invoice(
                saga.TenantId!.Value,
                subscription.Id,
                proration.NetAdjustment,
                subscription.Amount.Currency,
                $"{saga.IdempotencyKey}:invoice");
            invoice.SetBillingPeriod(subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd);
            invoice.SetDescription($"Plan change proration: {saga.FromPlanId} -> {saga.ToPlanId}");
            invoice.SetMetadata(
                $$"""{"sagaId":"{{saga.Id}}","fromPlanId":"{{saga.FromPlanId}}","toPlanId":"{{saga.ToPlanId}}","creditForUnused":{{proration.CreditForUnused:F2}},"chargeForNew":{{proration.ChargeForNew:F2}}}""");
            invoice.Issue(SystemClock.UtcNow);
            context.Set<Invoice>().Add(invoice);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            saga.RecordInvoice(invoice.Id);
            await RecordStepAsync(
                saga,
                InvoiceStep,
                "Succeeded",
                $"invoice {invoice.InvoiceNumber} issued for {proration.NetAdjustment:F2} {subscription.Amount.Currency}",
                cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await RecordStepAsync(
                saga,
                InvoiceStep,
                "Skipped",
                $"net adjustment {proration.NetAdjustment:F2} is not positive; credit carried on the saga record",
                cancellationToken)
                .ConfigureAwait(false);
        }

        // ── Step 3: Payment (external; compensated, never transactional) ───────────
        string? transactionId = null;
        string? chargedProviderId = null;
        if (proration.NetAdjustment > 0)
        {
            saga.AdvanceTo(PaymentStep);
            string gatewayPaymentMethodId;
            string gatewayProvider;
            try
            {
                (gatewayPaymentMethodId, gatewayProvider) = await ResolveGatewayPaymentMethodIdAsync(
                        request.PaymentMethodId ?? subscription.PaymentMethodId,
                        saga.TenantId!.Value,
                        subscription.CreatedByUserId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Reference validation happens before any gateway side effect.
                await CompensateAsync(saga, PaymentStep, exception.Message, invoice,
                    transactionId: null, cancellationToken).ConfigureAwait(false);
                return ToResult(saga);
            }

            chargedProviderId = gatewayProvider;
            // Transport exceptions must roll back the local transaction. They do not
            // prove that the provider rejected the charge; a retry uses the same key.
            var charge = await paymentGateway
                    .ProcessPaymentAsync(
                        new GatewayPaymentRequest(
                            $"{saga.IdempotencyKey}:charge",
                            proration.NetAdjustment,
                            subscription.Amount.Currency,
                            subscription.ExternalCustomerId,
                            gatewayPaymentMethodId,
                            $"Plan change proration for subscription {subscription.Id}",
                            new Dictionary<string, string>
                            {
                                ["changeIntent"] = saga.IdempotencyKey,
                                ["subscriptionId"] = subscription.Id.ToString()
                            },
                            GatewayProvider: gatewayProvider),
                        cancellationToken)
                    .ConfigureAwait(false);
            if (charge.FailureKind == GatewayFailureKind.OutcomeUnknown
                || charge.Status is GameGuild.Commerce.Payments.PaymentStatus.Pending
                    or GameGuild.Commerce.Payments.PaymentStatus.Processing
                    or GameGuild.Commerce.Payments.PaymentStatus.RequiresAction)
            {
                throw new InvalidOperationException("The provider has not confirmed the plan-change payment; retry the same change intent after reconciliation.");
            }

            if (!charge.Success || string.IsNullOrWhiteSpace(charge.TransactionId))
            {
                var reason = charge.ErrorMessage ?? charge.ErrorCode ?? "gateway charge failed";
                await CompensateAsync(
                    saga,
                    PaymentStep,
                    reason,
                    invoice,
                    transactionId: null,
                    cancellationToken)
                    .ConfigureAwait(false);
                return ToResult(saga);
            }

            transactionId = charge.TransactionId;
            chargedProviderId = charge.ProviderId ?? chargedProviderId;
            // An idempotent charge response can be cached even after a refund. Read
            // the current provider state before granting the paid plan on a replay.
            var confirmed = await paymentGateway.GetPaymentAsync(transactionId, cancellationToken, chargedProviderId)
                .ConfigureAwait(false);
            if (!confirmed.Success || confirmed.Status != GameGuild.Commerce.Payments.PaymentStatus.Succeeded)
            {
                throw new InvalidOperationException("The current provider payment is not settled; the subscription plan was not activated.");
            }
            saga.RecordPayment(transactionId);
            if (invoice is not null)
            {
                invoice.RecordPayment(
                    InvoiceGenerationService.DeriveProviderPaymentId(transactionId),
                    proration.NetAdjustment,
                    SystemClock.UtcNow);
            }

            await RecordStepAsync(
                saga,
                PaymentStep,
                "Succeeded",
                $"{chargedProviderId} charge {transactionId} settled {proration.NetAdjustment:F2} {subscription.Amount.Currency}",
                cancellationToken)
                .ConfigureAwait(false);
        }

        // ── Step 4: Activation (the actual plan change) ────────────────────────────
        saga.AdvanceTo(ActivationStep);
        try
        {
            subscription.ChangePlan(request.NewPlanId, newAmount, effectiveDate);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await CompensateAsync(
                saga,
                ActivationStep,
                exception.Message,
                invoice,
                transactionId,
                cancellationToken,
                providerId: chargedProviderId)
                .ConfigureAwait(false);
            return ToResult(saga);
        }

        // A persistence exception leaves commit outcome uncertain. Never refund and
        // then save the still-tracked new plan from the compensation path. Propagate
        // so the transaction owner rolls back all local writes; the provider charge
        // identity remains stable if the same intent is retried.
        await subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);
        saga.Complete(SystemClock.UtcNow);
        await RecordStepAsync(
            saga,
            ActivationStep,
            "Succeeded",
            $"subscription {subscription.Id} active on plan {request.NewPlanId}",
            cancellationToken)
            .ConfigureAwait(false);
        await billingEvents.PublishSubscriptionChangeCompletedAsync(saga, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Subscription change saga {SagaId} completed: subscription {SubscriptionId} moved to plan {ToPlanId} (net {Net} {Currency})",
            saga.Id,
            subscription.Id,
            saga.ToPlanId,
            saga.NetAdjustment,
            saga.Currency);

        return ToResult(saga);
    }

    /// <summary>
    ///     Applies compensating actions for a failed step, newest-effect-first: refund the
    ///     gateway charge when the failure happened after payment, void the issued invoice,
    ///     then record the failed terminal state and its durable event.
    /// </summary>
    private async Task CompensateAsync(
        SubscriptionChangeSaga saga,
        string failedStep,
        string reason,
        Invoice? invoice,
        string? transactionId,
        CancellationToken cancellationToken,
        string? providerId = null)
    {
        var compensations = new List<string>();

        if (transactionId is not null)
        {
            try
            {
                var refund = await paymentGateway
                    .ProcessRefundAsync(
                        new GatewayRefundRequest(
                            $"{saga.IdempotencyKey}:refund",
                            transactionId,
                            saga.NetAdjustment,
                            $"Subscription change saga {saga.Id} failed at {failedStep}: {reason}",
                            ProviderId: providerId,
                            Currency: saga.Currency),
                        cancellationToken)
                    .ConfigureAwait(false);
                compensations.Add(refund.Success
                    ? $"refund:{refund.RefundId}"
                    : $"refund-failed:{refund.ErrorMessage ?? refund.ErrorCode}");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                compensations.Add($"refund-threw:{exception.Message}");
            }
        }

        if (invoice is not null && invoice.Status is InvoiceStatus.Open or InvoiceStatus.Draft)
        {
            try
            {
                invoice.Void($"Subscription change saga {saga.Id} failed at {failedStep}");
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                compensations.Add($"invoice-voided:{invoice.InvoiceNumber}");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                compensations.Add($"invoice-void-failed:{exception.Message}");
            }
        }

        saga.BeginCompensation(failedStep, reason, SystemClock.UtcNow);
        saga.Fail(string.Join(", ", compensations));
        var journal = new SubscriptionChangeSagaStep(
            saga.Id,
            saga.Steps.Count + 1,
            failedStep,
            "Failed",
            $"reason={reason}; compensations=[{string.Join(", ", compensations)}]",
            SystemClock.UtcNow);
        saga.AppendStep(journal);
        context.Set<SubscriptionChangeSagaStep>().Add(journal);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await billingEvents
            .PublishSubscriptionChangeStepAsync(saga, failedStep, "Failed", reason, cancellationToken)
            .ConfigureAwait(false);
        await billingEvents.PublishSubscriptionChangeFailedAsync(saga, cancellationToken).ConfigureAwait(false);

        logger.LogWarning(
            "Subscription change saga {SagaId} failed at {FailedStep}: {Reason}; compensations: {Compensations}",
            saga.Id,
            failedStep,
            reason,
            saga.CompensationsApplied);
    }

    private async Task<(string Token, string Provider)> ResolveGatewayPaymentMethodIdAsync(
        Guid? localPaymentMethodId,
        Guid tenantId,
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        if (!localPaymentMethodId.HasValue)
        {
            throw new InvalidOperationException(
                "No payment method on the subscription and none supplied; cannot charge a positive net adjustment.");
        }

        var method = await paymentMethodRepository
            .GetByIdAsync(localPaymentMethodId.Value, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Payment method {localPaymentMethodId} not found.");

        if (method.TenantId != tenantId || method.UserId != ownerUserId || !method.IsChargeable)
        {
            throw new InvalidOperationException(
                $"Payment method {localPaymentMethodId} belongs to tenant {method.TenantId}, not {tenantId}.");
        }

        return (gatewayIdProtector.Unprotect(method.GatewayPaymentMethodId), method.GatewayProvider);
    }

    private async Task RecordStepAsync(
        SubscriptionChangeSaga saga,
        string step,
        string outcome,
        string detail,
        CancellationToken cancellationToken)
    {
        var journal = new SubscriptionChangeSagaStep(
            saga.Id,
            saga.Steps.Count + 1,
            step,
            outcome,
            detail,
            SystemClock.UtcNow);
        saga.AppendStep(journal);
        context.Set<SubscriptionChangeSagaStep>().Add(journal);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await billingEvents
            .PublishSubscriptionChangeStepAsync(saga, step, outcome, detail, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<SubscriptionChangeSaga?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var saga = await context.Set<SubscriptionChangeSaga>()
            .FirstOrDefaultAsync(candidate => candidate.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);
        if (saga is null)
        {
            return null;
        }

        var steps = await context.Set<SubscriptionChangeSagaStep>()
            .Where(step => step.SagaId == saga.Id)
            .OrderBy(step => step.Sequence)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var step in steps)
        {
            if (saga.Steps.All(existingStep => existingStep.Id != step.Id))
            {
                saga.AppendStep(step);
            }
        }

        return saga;
    }

    private static SubscriptionChangeSagaResult ToResult(SubscriptionChangeSaga saga) => new(
        saga.Id,
        saga.SubscriptionId,
        saga.Status,
        saga.FromPlanId,
        saga.ToPlanId,
        saga.NetAdjustment,
        saga.Currency,
        saga.InvoiceId,
        saga.PaymentTransactionId,
        saga.FailedStep,
        saga.FailureReason,
        saga.CompensationsApplied,
        saga.Steps
            .Select(step => new SubscriptionChangeSagaStepResult(step.Step, step.Outcome, step.Detail, step.OccurredAtUtc))
            .ToArray());

    /// <summary>
    ///     Unique key of one change intent: subscription, target plan and effective date.
    ///     Two orchestrations of the same intent resolve to the same saga row.
    /// </summary>
    public static string BuildIdempotencyKey(Guid subscriptionId, Guid newPlanId, DateTime effectiveDateUtc) =>
        $"subscription:{subscriptionId}:change:{newPlanId}:{effectiveDateUtc:yyyyMMdd}";
}

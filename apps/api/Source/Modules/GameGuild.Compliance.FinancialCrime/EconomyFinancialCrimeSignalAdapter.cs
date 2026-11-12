using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Risk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.FinancialCrime;

/// <summary>
/// The option-C economy hook (issue #1291 FC1): a decorator over Economy's trusted protected
/// operation authorizer — the single choke point every value-moving flow (top-up mint,
/// hard-to-soft conversion, transfer, payout execution, marketplace settlement) passes
/// through. After the inner authorizer commits an operation of one of those five capabilities,
/// the adapter (1) enqueues the operation as an AML signal on the module's own outbox — a
/// single durable insert in a fresh scope, so screening never blocks or fails the payment path —
/// and (2) consumes the subject's newest unconsumed Approved case decision bound to the
/// operation fingerprint, which is what links a money movement to the AML decision that
/// authorized it. Denials keep flowing from the inner authorizer untouched: the
/// <c>ComplianceUnavailable</c> fail-closed path (no evidence) stays exactly as it was, and a
/// Blocked or NeedsReview subject is denied with the risk diagnostics as before.
///
/// Port-inversion note (#1294 tenant-guard pattern): the adapter is declared in this module
/// and implements an Economy-owned interface — FinancialCrime already references Economy, so
/// no circular reference is created — while the registration happens in the host composition
/// (<c>AddFinancialCrimeEconomyIngestion</c>), where Economy's concrete default is resolvable.
/// </summary>
public sealed class EconomyFinancialCrimeSignalAdapter(
    EconomyTrustedProtectedOperationAuthorizer inner,
    IServiceScopeFactory scopeFactory,
    IOptions<FinancialCrimeEconomyHookOptions> hookOptions,
    TimeProvider timeProvider,
    ILogger<EconomyFinancialCrimeSignalAdapter>? logger = null) : IEconomyTrustedProtectedOperationAuthorizer
{
    private static readonly HashSet<EconomyValueMovementCapability> SignaledCapabilities =
    [
        EconomyValueMovementCapability.ConfirmHardCoinFunding,
        EconomyValueMovementCapability.ConvertHardToSoft,
        EconomyValueMovementCapability.Transfer,
        EconomyValueMovementCapability.PayoutExecution,
        EconomyValueMovementCapability.MarketplaceSettlement
    ];

    private readonly EconomyTrustedProtectedOperationAuthorizer _inner =
        inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly FinancialCrimeEconomyHookOptions _hook =
        (hookOptions ?? throw new ArgumentNullException(nameof(hookOptions))).Value;

    public async Task<TResult> ExecuteAsync<TResult>(
        Guid tenantId,
        Guid actorId,
        EconomyProtectedOperationIntent intent,
        Func<EconomyProtectedOperationAuthorization, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(operation);
        EconomyProtectedOperationAuthorization? authorization = null;
        var result = await _inner.ExecuteAsync(
            tenantId,
            actorId,
            intent,
            (granted, token) =>
            {
                // Capture the authorization the money path received: it carries the exact
                // operation fingerprint Economy computed, without this module reaching into
                // Economy internals to recompute it. The inner authorizer invokes the
                // operation on every success path, so a returned result implies a capture.
                authorization = granted;
                return operation(granted, token);
            },
            cancellationToken).ConfigureAwait(false);
        // The inner authorizer threw on denial, so reaching this point means value moved.
        if (_hook.Enabled && SignaledCapabilities.Contains(intent.Capability))
        {
            await ObserveComplianceAsync(tenantId, actorId, intent, authorization!, cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }

    private async ValueTask ObserveComplianceAsync(
        Guid tenantId,
        Guid actorId,
        EconomyProtectedOperationIntent intent,
        EconomyProtectedOperationAuthorization authorization,
        CancellationToken cancellationToken)
    {
        // A fresh scope on purpose: the money-path DbContext stays untouched whatever happens
        // here, and every failure below is contained — an AML observability failure must never
        // fail an operation that already committed.
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var now = timeProvider.GetUtcNow();
            var subjectId = intent.ProtectedSubjectId ?? actorId;
            var queue = scope.ServiceProvider.GetRequiredService<IFinancialCrimeSignalQueue>();
            await queue.EnqueueEconomyOperationAsync(new FinancialCrimeEconomyOperationSignal(
                tenantId,
                actorId,
                EconomySubjectReference.ForUser(tenantId, subjectId),
                intent.Capability.ToString(),
                intent.Amount.Units,
                intent.Amount.Currency.ToString(),
                // The counterparty reference when the operation names one: the destination
                // wallet for peer-shaped flows (transfer, settlement, payout).
                intent.DestinationWalletId == intent.SourceWalletId
                    ? null
                    : intent.DestinationWalletId.Value,
                intent.ProviderReferenceHash.Trim(),
                authorization.OperationFingerprint,
                intent.IdempotencyKey.Value,
                intent.RequestedAt), cancellationToken).ConfigureAwait(false);
            await TryConsumeDecisionAsync(
                scope.ServiceProvider, tenantId, subjectId, intent, authorization, now, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger?.LogWarning(exception,
                "Financial Crime signal enqueue failed for protected operation {Capability}; the operation itself was not affected.",
                intent.Capability);
        }
    }

    private async ValueTask TryConsumeDecisionAsync(
        IServiceProvider services,
        Guid tenantId,
        Guid subjectId,
        EconomyProtectedOperationIntent intent,
        EconomyProtectedOperationAuthorization authorization,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var plane = services.GetRequiredService<IFinancialCrimeControlPlane>();
        var subjectHash = EconomySubjectReference.ForUser(tenantId, subjectId);
        var decision = await plane.ReadLatestApprovedDecisionAsync(tenantId, subjectHash, now, cancellationToken)
            .ConfigureAwait(false);
        if (decision is null)
        {
            return;
        }

        try
        {
            await plane.ConsumeDecisionAsync(tenantId, decision.Id, authorization.OperationFingerprint, now, cancellationToken)
                .ConfigureAwait(false);
            logger?.LogInformation(
                "Financial Crime decision {DecisionId} was consumed by protected operation {Capability} (fingerprint {Fingerprint}).",
                decision.Id, intent.Capability, authorization.OperationFingerprint);
        }
        catch (FinancialCrimeConflictException)
        {
            // Another operation consumed the single-use decision first, or the window closed:
            // routine screening evidence still authorizes this operation.
        }
    }
}

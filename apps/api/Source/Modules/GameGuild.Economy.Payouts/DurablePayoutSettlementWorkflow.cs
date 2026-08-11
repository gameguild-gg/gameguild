using System.Security.Cryptography;
using System.Text;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;

namespace GameGuild.Economy.Payouts;

public sealed record DurablePayoutDispatchRequest(
    Guid OperationId,
    long ExpectedVersion,
    long FencingToken,
    long KillSwitchEpoch,
    string DispatchSnapshotHash,
    DateTimeOffset OccurredAt);

public sealed record DurablePayoutProviderEventRequest(
    PayoutProviderEvent ProviderEvent,
    RegisteredPostingAuthority Authority);

public interface IDurablePayoutSettlementWorkflow
{
    Task<PayoutOperation> BeginDispatchAsync(
        DurablePayoutDispatchRequest request,
        CancellationToken cancellationToken = default);

    Task<PayoutOperation> ApplyProviderEventAsync(
        DurablePayoutProviderEventRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists payout transitions after FIFO reservation. Provider callbacks cannot move value by
/// themselves: terminal settlement still requires an independently issued, single-use authority.
/// </summary>
public sealed class PostgreSqlDurablePayoutSettlementWorkflow(
    IApplicationDbContext dbContext,
    IPayoutOperationStore operations,
    IFifoFragmentReservationGateway reservations,
    IRegisteredPostingGateway postings,
    IPayoutProviderEvidenceVerifier providerEvidence) : IDurablePayoutSettlementWorkflow
{
    public async Task<PayoutOperation> BeginDispatchAsync(
        DurablePayoutDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateDispatchRequest(request);

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var operation = operations.Get(request.OperationId);
            if (operation.State == PayoutOperationState.Dispatching &&
                operation.Version == checked(request.ExpectedVersion + 1) &&
                string.Equals(operation.DispatchSnapshotHash, request.DispatchSnapshotHash.Trim(), StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return operation;
            }

            ValidateDispatchTransition(operation, request);
            var dispatching = operation.Transition(
                PayoutOperationState.Dispatching,
                request.OccurredAt,
                dispatchSnapshotHash: request.DispatchSnapshotHash.Trim());
            var persisted = operations.Update(dispatching, operation.Version);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return persisted;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<PayoutOperation> ApplyProviderEventAsync(
        DurablePayoutProviderEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateProviderEvent(request.ProviderEvent);
        var eventHash = ProviderEventHash(request.ProviderEvent);

        var replay = operations.FindProviderEvent(request.ProviderEvent.EventId, eventHash);
        if (replay is not null)
            return operations.Get(replay.OperationId);

        if (!providerEvidence.Verify(request.ProviderEvent))
            throw new PayoutEvidenceException("Provider payout event signature is invalid.");

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            replay = operations.FindProviderEvent(request.ProviderEvent.EventId, eventHash);
            if (replay is not null)
            {
                var persistedReplay = operations.Get(replay.OperationId);
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return persistedReplay;
            }

            var operation = operations.Get(request.ProviderEvent.OperationId);
            ValidateTerminalTransition(operation, request.ProviderEvent, request.Authority);
            var nextState = request.ProviderEvent.Outcome == PayoutProviderOutcome.Succeeded
                ? PayoutOperationState.Succeeded
                : PayoutOperationState.Failed;
            var nextReservationStatus = nextState == PayoutOperationState.Succeeded
                ? PersistedFragmentReservationStatus.Consumed
                : PersistedFragmentReservationStatus.Released;
            var postingKind = nextState == PayoutOperationState.Succeeded
                ? PostingTemplateKind.PayoutSuccess
                : PostingTemplateKind.PayoutFailure;
            var changed = operation.Transition(
                nextState,
                request.ProviderEvent.ObservedAt,
                providerPayoutId: request.ProviderEvent.ProviderPayoutId.Trim());

            postings.Post(new RegisteredPostingRequest(
                request.Authority,
                CreateTerminalPosting(operation, postingKind, request.ProviderEvent.ObservedAt)));
            var transitioned = reservations.Transition(
                operation.Id,
                PersistedFragmentReservationStatus.Reserved,
                nextReservationStatus,
                request.ProviderEvent.ObservedAt);
            if (transitioned <= 0)
                throw new PayoutStaleCommandException("Payout fragments are no longer reserved for terminal settlement.");

            operations.RecordProviderEvent(
                request.ProviderEvent.EventId,
                eventHash,
                changed,
                operation.Version,
                request.ProviderEvent.ObservedAt);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return changed;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static PostingRequest CreateTerminalPosting(
        PayoutOperation operation,
        PostingTemplateKind kind,
        DateTimeOffset occurredAt) => new(
        DeterministicPostingId(operation.Id, kind == PostingTemplateKind.PayoutSuccess ? "success" : "failure"),
        new PostingTemplate(kind, PostingTemplate.CurrentVersion),
        new IdempotencyKey($"{operation.IdempotencyKey.Value}:{kind}"),
        PostingAuthority.PayoutCoordinator,
        operation.ReserveVersion,
        operation.PolicyVersion,
        null,
        occurredAt,
        kind == PostingTemplateKind.PayoutSuccess
            ?
            [
                new PostingLine(1, EntrySide.Debit, EconomyAccountCode.PayoutPayableHard, operation.Amount, null, null, null),
                new PostingLine(2, EntrySide.Credit, EconomyAccountCode.ExternalClearingHard, operation.Amount, null, null, null)
            ]
            :
            [
                new PostingLine(1, EntrySide.Debit, EconomyAccountCode.PayoutPayableHard, operation.Amount, null, null, null),
                new PostingLine(2, EntrySide.Credit, EconomyAccountCode.EarnedHardLiability, operation.Amount, operation.WalletId, null, ProvenanceKind.EarnedHard)
            ]);

    private static void ValidateDispatchRequest(DurablePayoutDispatchRequest request)
    {
        if (request.OperationId == Guid.Empty)
            throw new ArgumentException("Payout operation ID is required.", nameof(request));
        if (request.ExpectedVersion <= 0 || request.FencingToken <= 0 || request.KillSwitchEpoch <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Payout dispatch control versions must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DispatchSnapshotHash);
        if (request.DispatchSnapshotHash.Trim().Length > 128)
            throw new ArgumentException("Dispatch snapshot hashes cannot exceed 128 characters.", nameof(request));
    }

    private static void ValidateDispatchTransition(PayoutOperation operation, DurablePayoutDispatchRequest request)
    {
        if (operation.State != PayoutOperationState.Reserved || operation.Version != request.ExpectedVersion ||
            operation.FencingToken != request.FencingToken || operation.KillSwitchEpoch != request.KillSwitchEpoch)
            throw new PayoutStaleCommandException("Payout dispatch command is stale or fenced.");
    }

    private static void ValidateProviderEvent(PayoutProviderEvent providerEvent)
    {
        ArgumentNullException.ThrowIfNull(providerEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.EventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.ProviderPayoutId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.ProviderAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.DestinationHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.EvidenceHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.Signature);
        if (providerEvent.OperationId == Guid.Empty)
            throw new ArgumentException("Provider payout events require an operation ID.", nameof(providerEvent));
        if (providerEvent.Outcome is not (PayoutProviderOutcome.Succeeded or PayoutProviderOutcome.Failed))
            throw new PayoutEvidenceException("Only terminal provider events can settle a payout.");
    }

    private static void ValidateTerminalTransition(
        PayoutOperation operation,
        PayoutProviderEvent providerEvent,
        RegisteredPostingAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(authority);
        if (operation.State is not (PayoutOperationState.Dispatching or PayoutOperationState.Ambiguous))
            throw new PayoutStaleCommandException("Provider terminal event is out of order.");
        if (authority.RiskDecisionId == operation.RiskDecisionId)
            throw new InvalidOperationException("Terminal payout settlement requires a distinct, single-use risk decision.");
        if (!string.Equals(providerEvent.ProviderAccountId, operation.ProviderAccountId, StringComparison.Ordinal) ||
            !string.Equals(providerEvent.DestinationHash, operation.DestinationHash, StringComparison.Ordinal))
            throw new PayoutProviderBindingException("Provider payout event is not bound to this operation.");
        if (providerEvent.ObservedAt < operation.CreatedAt)
            throw new PayoutEvidenceException("Provider payout event predates the payout operation.");
    }

    private static string ProviderEventHash(PayoutProviderEvent providerEvent)
    {
        var payload = string.Join('|',
            providerEvent.EventId.Trim(),
            providerEvent.OperationId.ToString("N"),
            ((int)providerEvent.Outcome).ToString(System.Globalization.CultureInfo.InvariantCulture),
            providerEvent.ProviderPayoutId.Trim(),
            providerEvent.ProviderAccountId.Trim(),
            providerEvent.DestinationHash.Trim(),
            providerEvent.EvidenceHash.Trim(),
            providerEvent.Signature.Trim(),
            providerEvent.ObservedAt.UtcDateTime.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static PostingId DeterministicPostingId(Guid operationId, string suffix)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{operationId:N}:payout:{suffix}"));
        return new PostingId(new Guid(bytes.AsSpan(0, 16)));
    }
}
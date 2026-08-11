using System.Security.Cryptography;
using System.Text;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;

namespace GameGuild.Economy.Treasury;

public sealed record DurableAdminWithdrawalReservationRequest(
    AdminWithdrawalRun Run,
    RegisteredPostingAuthority Authority);

public sealed record DurableAdminWithdrawalApprovalRequest(
    Guid RunId,
    long ExpectedVersion,
    Guid ApprovedBy,
    DateTimeOffset ApprovedAt);

public sealed record DurableAdminWithdrawalDispatchRequest(
    Guid RunId,
    long ExpectedVersion,
    long FencingToken,
    long ExecutionEpoch,
    string DispatchSnapshotHash,
    DateTimeOffset OccurredAt);

public sealed record DurableAdminWithdrawalProviderEventRequest(
    AdminWithdrawalProviderEvent ProviderEvent,
    RegisteredPostingAuthority Authority);

public interface IDurableAdminWithdrawalWorkflow
{
    Task<AdminWithdrawalRun> ReserveAsync(DurableAdminWithdrawalReservationRequest request, CancellationToken cancellationToken = default);
    Task<AdminWithdrawalRun> ApproveAsync(DurableAdminWithdrawalApprovalRequest request, CancellationToken cancellationToken = default);
    Task<AdminWithdrawalRun> BeginDispatchAsync(DurableAdminWithdrawalDispatchRequest request, CancellationToken cancellationToken = default);
    Task<AdminWithdrawalRun> ApplyProviderEventAsync(DurableAdminWithdrawalProviderEventRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// PostgreSQL-backed two-person platform treasury withdrawal lifecycle. Every movement remains
/// protected by a single-use registered-posting authority and terminal provider evidence.
/// </summary>
public sealed class PostgreSqlDurableAdminWithdrawalWorkflow(
    IApplicationDbContext dbContext,
    IAdminWithdrawalStore operations,
    IAdminWithdrawalAuditTrail audit,
    IFifoFragmentReservationGateway reservations,
    IRegisteredPostingGateway postings,
    IAdminWithdrawalProviderEvidenceVerifier providerEvidence) : IDurableAdminWithdrawalWorkflow
{
    public async Task<AdminWithdrawalRun> ReserveAsync(
        DurableAdminWithdrawalReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateNewRun(request.Run, request.Authority);
        var replay = operations.FindReplay(request.Run.IdempotencyKey.Value, request.Run.RequestHash);
        if (replay is not null)
            return replay;

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            replay = operations.FindReplay(request.Run.IdempotencyKey.Value, request.Run.RequestHash);
            if (replay is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return replay;
            }
            if (operations.FindPeriod(request.Run.PeriodStart) is not null)
                throw new AdminWithdrawalOverlapException("A withdrawal run already owns this monthly period.");

            operations.Add(request.Run);
            var fragments = reservations.Reserve(new FifoFragmentReservationRequest(
                request.Run.Id,
                request.Run.PlatformFeeWalletId,
                CurrencyCode.HardCoin,
                ProvenanceKind.EarnedHard,
                request.Run.Amount,
                PersistedFragmentReservationPurpose.AdminWithdrawal,
                request.Run.CreatedAt));
            if (fragments.Sum(fragment => fragment.Amount.Units) != request.Run.Amount.Units)
                throw new AdminWithdrawalEligibilityException("Administrative withdrawal FIFO reservations do not match the requested amount.");

            postings.Post(new RegisteredPostingRequest(
                request.Authority,
                CreateReservationPosting(request.Run),
                fragments.Select(fragment => new RegisteredPostingAllocation(
                    1,
                    fragment.ParentLotId,
                    fragment.Amount.Units,
                    [fragment.Range]))
                    .ToArray()));
            audit.Append(request.Run.Id, "reserved", request.Run.RequestedBy, request.Run.RequestHash, request.Run.CreatedAt);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return request.Run;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<AdminWithdrawalRun> ApproveAsync(
        DurableAdminWithdrawalApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateApprovalRequest(request);
        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var run = operations.Get(request.RunId);
            if (run.State == AdminWithdrawalRunState.Approved && run.Version == checked(request.ExpectedVersion + 1) && run.ApprovedBy == request.ApprovedBy)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return run;
            }
            if (run.State != AdminWithdrawalRunState.PendingApproval || run.Version != request.ExpectedVersion)
                throw new AdminWithdrawalStaleCommandException("The withdrawal approval command is stale.");
            if (run.RequestedBy == request.ApprovedBy)
                throw new AdminWithdrawalApprovalException("The withdrawal requester cannot approve the same run.");

            var approved = run with
            {
                ApprovedBy = request.ApprovedBy,
                State = AdminWithdrawalRunState.Approved,
                Version = checked(run.Version + 1),
                UpdatedAt = request.ApprovedAt
            };
            operations.Update(approved, run.Version);
            audit.Append(run.Id, "approved", request.ApprovedBy, run.RequestHash, request.ApprovedAt);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return approved;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<AdminWithdrawalRun> BeginDispatchAsync(
        DurableAdminWithdrawalDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateDispatchRequest(request);
        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var run = operations.Get(request.RunId);
            if (run.State == AdminWithdrawalRunState.Dispatching && run.Version == checked(request.ExpectedVersion + 1) &&
                string.Equals(run.DispatchSnapshotHash, request.DispatchSnapshotHash.Trim(), StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return run;
            }
            if (run.State != AdminWithdrawalRunState.Approved || run.Version != request.ExpectedVersion ||
                run.FencingToken != request.FencingToken || run.ExecutionEpoch != request.ExecutionEpoch ||
                !run.ApprovedBy.HasValue || run.ApprovedBy == run.RequestedBy)
                throw new AdminWithdrawalStaleCommandException("Admin withdrawal dispatch command is stale, unapproved, or fenced.");

            var dispatching = run with
            {
                State = AdminWithdrawalRunState.Dispatching,
                Version = checked(run.Version + 1),
                DispatchSnapshotHash = request.DispatchSnapshotHash.Trim(),
                UpdatedAt = request.OccurredAt
            };
            operations.Update(dispatching, run.Version);
            audit.Append(run.Id, "dispatching", run.ApprovedBy, dispatching.DispatchSnapshotHash, request.OccurredAt);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return dispatching;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task<AdminWithdrawalRun> ApplyProviderEventAsync(
        DurableAdminWithdrawalProviderEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateProviderEventShape(request.ProviderEvent);
        var eventHash = ProviderEventHash(request.ProviderEvent);
        var replayRunId = operations.FindProviderEvent(request.ProviderEvent.EventId, eventHash);
        if (replayRunId.HasValue)
            return operations.Get(replayRunId.Value);
        if (!providerEvidence.Verify(request.ProviderEvent))
            throw new AdminWithdrawalEvidenceException("Provider withdrawal event signature is invalid.");

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            replayRunId = operations.FindProviderEvent(request.ProviderEvent.EventId, eventHash);
            if (replayRunId.HasValue)
            {
                var replay = operations.Get(replayRunId.Value);
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return replay;
            }

            var run = operations.Get(request.ProviderEvent.RunId);
            ValidateTerminalEvent(run, request.ProviderEvent, request.Authority);
            var succeeded = request.ProviderEvent.Outcome == AdminWithdrawalProviderOutcome.Succeeded;
            var postingKind = succeeded
                ? PostingTemplateKind.AdminWithdrawalSuccess
                : PostingTemplateKind.AdminWithdrawalFailure;
            var terminal = run with
            {
                State = succeeded ? AdminWithdrawalRunState.Succeeded : AdminWithdrawalRunState.Failed,
                ProviderTransferId = request.ProviderEvent.ProviderTransferId.Trim(),
                Version = checked(run.Version + 1),
                UpdatedAt = request.ProviderEvent.ObservedAt
            };

            postings.Post(new RegisteredPostingRequest(
                request.Authority,
                CreateTerminalPosting(run, postingKind, request.ProviderEvent.ObservedAt)));
            var changedReservations = reservations.Transition(
                run.Id,
                PersistedFragmentReservationStatus.Reserved,
                succeeded ? PersistedFragmentReservationStatus.Consumed : PersistedFragmentReservationStatus.Released,
                request.ProviderEvent.ObservedAt);
            if (changedReservations <= 0)
                throw new AdminWithdrawalStaleCommandException("Administrative withdrawal fragments are no longer reserved.");

            operations.RecordProviderEvent(request.ProviderEvent.EventId, eventHash, terminal, run.Version);
            audit.Append(run.Id, succeeded ? "succeeded" : "failed", null, eventHash, request.ProviderEvent.ObservedAt);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return terminal;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static PostingRequest CreateReservationPosting(AdminWithdrawalRun run) => new(
        new PostingId(run.Id),
        new PostingTemplate(PostingTemplateKind.AdminWithdrawalReservation, PostingTemplate.CurrentVersion),
        run.IdempotencyKey,
        PostingAuthority.Administrator,
        run.ReserveVersion,
        run.PolicyVersion,
        null,
        run.CreatedAt,
        [
            new PostingLine(1, EntrySide.Debit, EconomyAccountCode.PlatformHardTreasury, run.Amount, null, null, null),
            new PostingLine(2, EntrySide.Credit, EconomyAccountCode.AdminWithdrawalPayableHard, run.Amount, null, null, null)
        ]);

    private static PostingRequest CreateTerminalPosting(AdminWithdrawalRun run, PostingTemplateKind kind, DateTimeOffset occurredAt) => new(
        DeterministicPostingId(run.Id, kind == PostingTemplateKind.AdminWithdrawalSuccess ? "success" : "failure"),
        new PostingTemplate(kind, PostingTemplate.CurrentVersion),
        new IdempotencyKey($"{run.IdempotencyKey.Value}:{kind}"),
        PostingAuthority.Administrator,
        run.ReserveVersion,
        run.PolicyVersion,
        null,
        occurredAt,
        kind == PostingTemplateKind.AdminWithdrawalSuccess
            ?
            [
                new PostingLine(1, EntrySide.Debit, EconomyAccountCode.AdminWithdrawalPayableHard, run.Amount, null, null, null),
                new PostingLine(2, EntrySide.Credit, EconomyAccountCode.ExternalClearingHard, run.Amount, null, null, null)
            ]
            :
            [
                new PostingLine(1, EntrySide.Debit, EconomyAccountCode.AdminWithdrawalPayableHard, run.Amount, null, null, null),
                new PostingLine(2, EntrySide.Credit, EconomyAccountCode.PlatformHardTreasury, run.Amount, null, null, null)
            ]);

    private static void ValidateNewRun(AdminWithdrawalRun run, RegisteredPostingAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(authority);
        if (run.Id == Guid.Empty || run.RequestedBy == Guid.Empty || run.PlatformFeeWalletId.Value == Guid.Empty)
            throw new ArgumentException("Run, requester, and platform fee wallet identities are required.", nameof(run));
        if (run.PeriodStart.Day != 1)
            throw new ArgumentException("Withdrawal period must start on the first day of a month.", nameof(run));
        if (run.State != AdminWithdrawalRunState.PendingApproval || run.Version != 1 || run.ApprovedBy.HasValue)
            throw new InvalidOperationException("Only a new, unapproved withdrawal run can reserve platform treasury value.");
        if (run.Amount.Currency != CurrencyCode.HardCoin || run.Amount.Units <= 0)
            throw new AdminWithdrawalEligibilityException("Administrative withdrawals require a positive hard-coin amount.");
        if (run.RequestedBy != authority.ActorId)
            throw new InvalidOperationException("The reservation authority must belong to the withdrawal requester.");
        if (run.FencingToken <= 0 || run.ExecutionEpoch <= 0 || run.ReserveAuthorizationEpoch <= 0)
            throw new ArgumentOutOfRangeException(nameof(run), "Administrative withdrawal control versions must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(run.RequestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(run.SourceAssetKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(run.DestinationHash);
    }

    private static void ValidateApprovalRequest(DurableAdminWithdrawalApprovalRequest request)
    {
        if (request.RunId == Guid.Empty || request.ApprovedBy == Guid.Empty)
            throw new ArgumentException("Withdrawal run and approver identities are required.", nameof(request));
        if (request.ExpectedVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "The withdrawal version must be positive.");
    }

    private static void ValidateDispatchRequest(DurableAdminWithdrawalDispatchRequest request)
    {
        if (request.RunId == Guid.Empty)
            throw new ArgumentException("Withdrawal run ID is required.", nameof(request));
        if (request.ExpectedVersion <= 0 || request.FencingToken <= 0 || request.ExecutionEpoch <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Administrative withdrawal control versions must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(request.DispatchSnapshotHash);
        if (request.DispatchSnapshotHash.Trim().Length > 128)
            throw new ArgumentException("Dispatch snapshot hashes cannot exceed 128 characters.", nameof(request));
    }

    private static void ValidateProviderEventShape(AdminWithdrawalProviderEvent providerEvent)
    {
        ArgumentNullException.ThrowIfNull(providerEvent);
        if (providerEvent.RunId == Guid.Empty)
            throw new ArgumentException("Provider withdrawal events require a run ID.", nameof(providerEvent));
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.EventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.ProviderTransferId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.SourceAssetKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.DestinationHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.EvidenceHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEvent.Signature);
        if (providerEvent.Outcome is not (AdminWithdrawalProviderOutcome.Succeeded or AdminWithdrawalProviderOutcome.Failed))
            throw new AdminWithdrawalEvidenceException("Only a terminal provider event can complete an administrative withdrawal.");
    }

    private static void ValidateTerminalEvent(
        AdminWithdrawalRun run,
        AdminWithdrawalProviderEvent providerEvent,
        RegisteredPostingAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(authority);
        if (run.State is not (AdminWithdrawalRunState.Dispatching or AdminWithdrawalRunState.Ambiguous))
            throw new AdminWithdrawalStaleCommandException("Provider terminal evidence is out of order.");
        if (providerEvent.FencingToken != run.FencingToken || providerEvent.ExecutionEpoch != run.ExecutionEpoch ||
            providerEvent.Amount != run.Amount ||
            !string.Equals(providerEvent.SourceAssetKey, run.SourceAssetKey, StringComparison.Ordinal) ||
            !string.Equals(providerEvent.DestinationHash, run.DestinationHash, StringComparison.Ordinal) ||
            (!string.IsNullOrWhiteSpace(run.ProviderTransferId) &&
             !string.Equals(providerEvent.ProviderTransferId, run.ProviderTransferId, StringComparison.Ordinal)))
            throw new AdminWithdrawalEvidenceException("Provider withdrawal event is not bound to the fenced run.");
        if (providerEvent.ObservedAt < run.CreatedAt)
            throw new AdminWithdrawalEvidenceException("Provider withdrawal event predates the run.");
    }

    private static string ProviderEventHash(AdminWithdrawalProviderEvent providerEvent)
    {
        var payload = string.Join('|',
            providerEvent.EventId.Trim(), providerEvent.RunId.ToString("N"),
            ((int)providerEvent.Outcome).ToString(System.Globalization.CultureInfo.InvariantCulture),
            providerEvent.ProviderTransferId.Trim(), providerEvent.FencingToken.ToString(System.Globalization.CultureInfo.InvariantCulture),
            providerEvent.ExecutionEpoch.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ((int)providerEvent.Amount.Currency).ToString(System.Globalization.CultureInfo.InvariantCulture),
            providerEvent.Amount.Units.ToString(System.Globalization.CultureInfo.InvariantCulture),
            providerEvent.SourceAssetKey.Trim(), providerEvent.DestinationHash.Trim(), providerEvent.EvidenceHash.Trim(),
            providerEvent.Signature.Trim(), providerEvent.ObservedAt.UtcDateTime.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static PostingId DeterministicPostingId(Guid runId, string suffix)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{runId:N}:admin-withdrawal:{suffix}"));
        return new PostingId(new Guid(bytes.AsSpan(0, 16)));
    }
}
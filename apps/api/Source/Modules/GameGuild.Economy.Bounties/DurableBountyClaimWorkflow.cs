using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Bounties;

/// <summary>
/// Immutable arguments accepted by the database-only terminal claim writer. The journal posting
/// is created first in the same transaction and is revalidated by the procedure before it moves
/// the escrow lots or terminal state.
/// </summary>
public sealed record BountyClaimTerminalWriteCommand(
    BountyId BountyId,
    Guid ClaimantId,
    WalletId ClaimantWalletId,
    IdempotencyKey IdempotencyKey,
    PostingId PostingId,
    Guid RiskDecisionId,
    string EvidenceHash,
    DateTimeOffset ClaimedAt);

public interface IBountyTerminalClaimWriter
{
    void Complete(BountyClaimTerminalWriteCommand command);
}

/// <summary>
/// No generic terminal-state mutation is available to the application writer. This procedure
/// validates the accepted BountyClaim posting, consumes escrow lots, materializes output lots,
/// and appends the immutable terminal evidence atomically.
/// </summary>
public sealed class PostgreSqlBountyTerminalClaimWriter : IBountyTerminalClaimWriter
{
    private readonly DbContext _db;

    public PostgreSqlBountyTerminalClaimWriter(IApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _db = context as DbContext ?? throw new InvalidOperationException(
            "PostgreSQL bounty terminal persistence requires the application's relational DbContext.");
    }

    public void Complete(BountyClaimTerminalWriteCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Validate(command);
        try
        {
            _db.Database.ExecuteSqlInterpolated($"""
                SELECT economy_private.complete_bounty_claim_v1(
                    {command.BountyId.Value},
                    {command.ClaimantId},
                    {command.ClaimantWalletId.Value},
                    {command.IdempotencyKey.Value},
                    {command.PostingId.Value},
                    {command.RiskDecisionId},
                    {command.EvidenceHash.Trim()},
                    {command.ClaimedAt});
                """);
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new RegisteredPostingRejectedException(
                "The durable bounty claim writer rejected the terminal settlement.", exception);
        }
    }

    private static void Validate(BountyClaimTerminalWriteCommand command)
    {
        if (command.ClaimantId == Guid.Empty)
            throw new ArgumentException("Claimant ID is required.", nameof(command));
        if (command.RiskDecisionId == Guid.Empty)
            throw new ArgumentException("Claim risk decision is required.", nameof(command));
        ArgumentException.ThrowIfNullOrWhiteSpace(command.EvidenceHash);
        if (command.EvidenceHash.Trim().Length > 128)
            throw new ArgumentException("Claim evidence hashes cannot exceed 128 characters.", nameof(command));
    }

    private static bool IsDatabaseFailure(Exception exception) =>
        exception is DbUpdateException or InvalidOperationException ||
        exception.GetBaseException() is System.Data.Common.DbException;
}

public interface IDurableBountyClaimWorkflow
{
    Task<PersistedBountyTerminalEvent> ClaimAsync(
        DurableBountyClaimRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Couples an accepted registered BountyClaim posting to the only specialized terminal writer.
/// It performs no direct bounty or lot mutation itself.
/// </summary>
public sealed class PostgreSqlDurableBountyClaimWorkflow(
    IApplicationDbContext dbContext,
    IBountyEscrowStore escrows,
    IBountyTerminalEventStore terminals,
    IRegisteredPostingGateway postings,
    IBountyTerminalClaimWriter terminalWriter) : IDurableBountyClaimWorkflow
{
    public async Task<PersistedBountyTerminalEvent> ClaimAsync(
        DurableBountyClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var replay = terminals.FindByIdempotency(request.IdempotencyKey);
        if (replay is not null)
            return EnsureReplayMatches(replay, request);

        var initialEscrow = escrows.Get(request.BountyId);
        EnsureClaimable(initialEscrow, request);

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            replay = terminals.FindByIdempotency(request.IdempotencyKey);
            if (replay is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return EnsureReplayMatches(replay, request);
            }

            var escrow = escrows.Get(request.BountyId);
            EnsureClaimable(escrow, request);
            var receipt = postings.Post(BountyClaimPostingFactory.Create(escrow, request));
            terminalWriter.Complete(new BountyClaimTerminalWriteCommand(
                request.BountyId,
                request.ClaimantId,
                request.ClaimantWalletId,
                request.IdempotencyKey,
                receipt.PostingId,
                request.Authority.RiskDecisionId,
                request.EvidenceHash,
                request.ClaimedAt));
            var completed = terminals.FindByIdempotency(request.IdempotencyKey)
                ?? throw new RegisteredPostingRejectedException(
                    "The durable bounty claim writer did not persist terminal evidence.");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return EnsureReplayMatches(completed, request);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static PersistedBountyTerminalEvent EnsureReplayMatches(
        PersistedBountyTerminalEvent replay,
        DurableBountyClaimRequest request)
    {
        if (replay.BountyId != request.BountyId || replay.Status != BountyStatus.Claimed ||
            replay.ActorId != request.ClaimantId || replay.DestinationWalletId != request.ClaimantWalletId ||
            replay.RiskDecisionId != request.Authority.RiskDecisionId)
            throw new BountyIdempotencyConflictException(
                "Bounty claim idempotency key is bound to another terminal outcome.");
        return replay;
    }

    private static void EnsureClaimable(PersistedBountyEscrow escrow, DurableBountyClaimRequest request)
    {
        if (escrow.Status != BountyStatus.Open)
            throw new BountyTerminalConflictException("The bounty already has a terminal outcome.");
        if (request.ClaimedAt >= escrow.ExpiresAt)
            throw new BountyExpiredException("The bounty can no longer be claimed.");
        if (request.ClaimantId == escrow.PosterId || request.ClaimantWalletId == escrow.PosterWalletId ||
            request.ClaimantWalletId == escrow.EscrowWalletId)
            throw new BountyClaimIneligibleException("A poster cannot claim their own bounty.");
        if (escrow.Fragments.Count == 0 || escrow.Fragments.Any(fragment => fragment.EscrowLotId is null))
            throw new RegisteredPostingRejectedException("Bounty escrow lots are not fully materialized.");
    }

    private static void ValidateRequest(DurableBountyClaimRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ClaimantId == Guid.Empty)
            throw new ArgumentException("Claimant ID is required.", nameof(request));
        if (request.Authority.ActorId != request.ClaimantId)
            throw new ArgumentException("The bounty claim authority must be the claimant.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EvidenceHash);
        if (request.EvidenceHash.Trim().Length > 128)
            throw new ArgumentException("Claim evidence hashes cannot exceed 128 characters.", nameof(request));
    }
}

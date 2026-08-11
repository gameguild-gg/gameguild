using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;

namespace GameGuild.Economy.Payouts;

public sealed record DurablePayoutReservationRequest(
    PayoutOperation Operation,
    RegisteredPostingAuthority Authority);

public interface IDurablePayoutReservationWorkflow
{
    Task<PayoutOperation> ReserveAsync(
        DurablePayoutReservationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class PostgreSqlDurablePayoutReservationWorkflow(
    IApplicationDbContext dbContext,
    IPayoutOperationStore operations,
    IFifoFragmentReservationGateway reservations,
    IRegisteredPostingGateway postings) : IDurablePayoutReservationWorkflow
{
    public async Task<PayoutOperation> ReserveAsync(
        DurablePayoutReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var operation = request.Operation;
        var replay = operations.FindReplay(operation.IdempotencyKey.Value, operation.RequestHash);
        if (replay is not null)
            return replay;

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            replay = operations.FindReplay(operation.IdempotencyKey.Value, operation.RequestHash);
            if (replay is not null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return replay;
            }

            operations.Add(operation);
            var fragments = reservations.Reserve(new FifoFragmentReservationRequest(
                operation.Id,
                operation.WalletId,
                CurrencyCode.HardCoin,
                ProvenanceKind.EarnedHard,
                operation.Amount,
                PersistedFragmentReservationPurpose.Payout,
                operation.CreatedAt));
            if (fragments.Sum(fragment => fragment.Amount.Units) != operation.Amount.Units)
                throw new RegisteredPostingRejectedException("Payout FIFO reservations do not match the requested hard-coin amount.");

            postings.Post(new RegisteredPostingRequest(
                request.Authority,
                CreateReservationPosting(operation),
                fragments.Select(fragment => new RegisteredPostingAllocation(
                    1,
                    fragment.ParentLotId,
                    fragment.Amount.Units,
                    [fragment.Range]))
                    .ToArray()));

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return operation;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private static PostingRequest CreateReservationPosting(PayoutOperation operation) => new(
        new PostingId(operation.Id),
        new PostingTemplate(PostingTemplateKind.PayoutReservation, PostingTemplate.CurrentVersion),
        operation.IdempotencyKey,
        PostingAuthority.PayoutCoordinator,
        operation.ReserveVersion,
        operation.PolicyVersion,
        null,
        operation.CreatedAt,
        [
            new PostingLine(
                1,
                EntrySide.Debit,
                EconomyAccountCode.EarnedHardLiability,
                operation.Amount,
                operation.WalletId,
                null,
                ProvenanceKind.EarnedHard),
            new PostingLine(
                2,
                EntrySide.Credit,
                EconomyAccountCode.PayoutPayableHard,
                operation.Amount,
                null,
                null,
                null)
        ]);

    private static void Validate(DurablePayoutReservationRequest request)
    {
        var operation = request.Operation;
        var authority = request.Authority;
        if (operation.Id == Guid.Empty)
            throw new ArgumentException("Payout operation ID is required.", nameof(request));
        if (operation.State != PayoutOperationState.Reserved || operation.Version != 1)
            throw new InvalidOperationException("Only a new reserved payout operation can create an immutable reservation posting.");
        if (operation.Amount.Currency != CurrencyCode.HardCoin || operation.Amount.Units <= 0)
            throw new ArgumentException("Payout reservations require a positive hard-coin amount.", nameof(request));
        if (operation.ActorId != authority.ActorId || operation.RiskDecisionId != authority.RiskDecisionId)
            throw new InvalidOperationException("Payout operation and protected posting authority must use the same actor and risk decision.");
        if (operation.KillSwitchEpoch <= 0 || operation.FencingToken <= 0 || operation.ReserveAuthorizationEpoch <= 0)
            throw new ArgumentException("Payout reservation control epochs must be positive.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.RequestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.ProviderAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.DestinationHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.ProviderBindingHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.EligibilityHash);
    }
}
using System.Data;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Persistence;
using GameGuild.Economy.Risk;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Payouts;

public sealed record DurablePayoutReservationRequest(
    PayoutOperation Operation,
    string SubjectReference,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string ReauthenticationEvidenceHash,
    string OperationFingerprint,
    string ProviderHash);

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
    IEconomyCapabilityAuthorizationService capabilityAuthorization,
    IPayoutAuthorizationEvidenceWriter authorizationEvidence,
    IRegisteredPostingCapabilityResolver capabilityResolver,
    IRegisteredPostingGateway postings) : IDurablePayoutReservationWorkflow
{
    private const string ReservationCapabilityName = "payout-reservation";

    public async Task<PayoutOperation> ReserveAsync(
        DurablePayoutReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var operation = request.Operation;
        var replay = operations.FindReplay(operation.TenantId, operation.IdempotencyKey.Value, operation.RequestHash);
        if (replay is not null)
            return replay;
        return await PostgreSqlTransactionExecutor.ExecuteAsync(
            dbContext, IsolationLevel.ReadCommitted, async _ =>
        {
            replay = operations.FindReplay(operation.TenantId, operation.IdempotencyKey.Value, operation.RequestHash);
            if (replay is not null)
                return replay;

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

            var rootHashes = fragments
                .Select(fragment => Hash(fragment.RootSourceStampId.Value.ToString("N")))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            var receipt = await capabilityAuthorization.AuthorizeAndConsumeAsync(
                new EconomyCapabilityEvaluationContext(
                    operation.TenantId,
                    operation.ActorId,
                    request.SubjectReference.Trim(),
                    request.JurisdictionCode.Trim().ToUpperInvariant(),
                    EconomyValueMovementCapability.PayoutExecution,
                    request.RiskDecisionId,
                    request.OperationFingerprint.Trim(),
                    request.ProviderHash.Trim(),
                    operation.DestinationHash,
                    rootHashes,
                    operation.CreatedAt),
                cancellationToken).ConfigureAwait(false);
            if (receipt.TenantId != operation.TenantId || receipt.ActorId != operation.ActorId ||
                receipt.RiskDecisionId != request.RiskDecisionId ||
                receipt.PolicyVersion != operation.PolicyVersion.Value ||
                receipt.ReserveVersion != operation.ReserveVersion.Value ||
                !string.Equals(receipt.ProviderHash, request.ProviderHash.Trim(), StringComparison.Ordinal) ||
                !string.Equals(receipt.DestinationHash, operation.DestinationHash, StringComparison.Ordinal) ||
                !receipt.SourceRootHashes.SequenceEqual(rootHashes, StringComparer.Ordinal))
                throw new InvalidOperationException(
                    "The payout capability receipt does not match the durable reservation snapshot.");
            var authority = await capabilityResolver.ResolveAuthorityAsync(
                ReservationCapabilityName,
                PostingTemplateKind.PayoutReservation,
                receipt,
                cancellationToken).ConfigureAwait(false);
            if (authority.TenantId != operation.TenantId || authority.ActorId != operation.ActorId ||
                authority.RiskDecisionId != receipt.RiskDecisionId)
                throw new InvalidOperationException(
                    "The registered posting authority does not match the payout actor and tenant.");
            var authorizedOperation = operation with { KillSwitchEpoch = receipt.KillSwitchEpoch };
            operations.Add(authorizedOperation);
            await authorizationEvidence.AppendAsync(
                new PayoutAuthorizationEvidence(
                    authorizedOperation.Id,
                    authorizedOperation.TenantId,
                    authorizedOperation.ActorId,
                    PayoutAuthorizationPhase.Reservation,
                    authorizedOperation.RiskDecisionId,
                    request.ReauthenticationEvidenceHash.Trim(),
                    Hash(request.OperationFingerprint.Trim()),
                    receipt.Id,
                    receipt.ReceiptHash,
                    authorizedOperation.CreatedAt),
                cancellationToken).ConfigureAwait(false);

            postings.Post(new RegisteredPostingRequest(
                authority,
                CreateReservationPosting(authorizedOperation),
                fragments.Select(fragment => new RegisteredPostingAllocation(
                    1,
                    fragment.ParentLotId,
                    fragment.Amount.Units,
                    [fragment.Range]))
                    .ToArray()));

            await Task.CompletedTask;
            return authorizedOperation;
        }, cancellationToken).ConfigureAwait(false);
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
        if (operation.Id == Guid.Empty)
            throw new ArgumentException("Payout operation ID is required.", nameof(request));
        if (operation.TenantId == Guid.Empty)
            throw new ArgumentException("Payout operation tenant ID is required.", nameof(request));
        if (operation.State != PayoutOperationState.Reserved || operation.Version != 1)
            throw new InvalidOperationException("Only a new reserved payout operation can create an immutable reservation posting.");
        if (operation.Amount.Currency != CurrencyCode.HardCoin || operation.Amount.Units <= 0)
            throw new ArgumentException("Payout reservations require a positive hard-coin amount.", nameof(request));
        if (operation.RiskDecisionId != request.RiskDecisionId)
            throw new InvalidOperationException("Payout operation and capability request must use the same risk decision.");
        if (operation.KillSwitchEpoch < 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Payout kill-switch epochs cannot be negative.");
        if (operation.FencingToken <= 0 || operation.ReserveAuthorizationEpoch <= 0)
            throw new ArgumentException("Payout reservation control epochs must be positive.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SubjectReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JurisdictionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ReauthenticationEvidenceHash);
        if (request.ReauthenticationEvidenceHash.Trim().Length != 64)
            throw new ArgumentException(
                "Payout reauthentication evidence hashes must contain 64 characters.", nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProviderHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.RequestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.ProviderAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.DestinationHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.ProviderBindingHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation.EligibilityHash);
    }

    private static string Hash(string value) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

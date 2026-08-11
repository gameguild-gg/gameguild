using System.Data.Common;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Funding;

public sealed record PersistedHardCoinFundingObservation(
    ObserveHardCoinTopUpCommand Command,
    Guid ActorId,
    Guid TenantId,
    PolicyVersion PolicyVersion);

public sealed record PersistedHardCoinFundingConfirmation(
    ConfirmObservedTopUpCommand Command,
    RegisteredPostingAuthority Authority);

public interface IHardCoinFundingGateway
{
    HardCoinFundingClaim Observe(PersistedHardCoinFundingObservation request);

    RegisteredPostingReceipt Confirm(PersistedHardCoinFundingConfirmation request);
}

public sealed class PostgreSqlHardCoinFundingGateway : IHardCoinFundingGateway
{
    private readonly DbContext _db;

    public PostgreSqlHardCoinFundingGateway(IApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _db = context as DbContext
            ?? throw new InvalidOperationException(
                "Persistent Economy funding requires the application's relational DbContext.");
    }

    public HardCoinFundingClaim Observe(PersistedHardCoinFundingObservation request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Command);
        if (request.ActorId == Guid.Empty) throw new ArgumentException("Actor ID is required.", nameof(request));
        if (request.TenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(request));
        if (request.PolicyVersion.Value <= 0) throw new ArgumentOutOfRangeException(nameof(request));

        var command = request.Command;
        var claim = HardCoinFundingClaim.Observe(
            command.SourceId,
            command.WalletId,
            command.ProviderLeg,
            command.Evidence,
            command.AuthoritativeUsdMinorUnits,
            command.ObservedAt);
        var source = SourceEvidence.Observe(
            command.SourceId,
            command.ProviderLeg.Provider,
            command.ProviderLeg.Key,
            command.Evidence,
            command.ObservedAt);

        try
        {
            _db.Database.ExecuteSqlInterpolated($"""
                SELECT economy_private.observe_hard_coin_top_up_v1(
                    {command.SourceId.Value},
                    {command.WalletId.Value},
                    {command.ProviderLeg.Provider},
                    {command.ProviderLeg.Environment},
                    {command.ProviderLeg.ConnectedAccount},
                    {command.ProviderLeg.ProviderObject},
                    {command.ProviderLeg.MonetaryLeg},
                    {claim.Amount.Units},
                    {source.EvidenceHash},
                    {claim.Events[0].EvidenceHash},
                    {command.ObservedAt},
                    {request.ActorId},
                    {request.TenantId},
                    {request.PolicyVersion.Value});
                """);
            return claim;
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new RegisteredPostingRejectedException(
                "The persistent Economy funding writer rejected the observed provider fact.",
                exception);
        }
    }

    public RegisteredPostingReceipt Confirm(PersistedHardCoinFundingConfirmation request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Command);
        ArgumentNullException.ThrowIfNull(request.Authority);

        var command = request.Command;
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Evidence);

        var source = _db.Set<EconomySourceStampRow>()
            .AsNoTracking()
            .SingleOrDefault(row => row.Id == command.SourceId.Value)
            ?? throw new RegisteredPostingRejectedException("The observed funding source was not found.");
        var funding = _db.Set<EconomyFundingClaimRow>()
            .AsNoTracking()
            .SingleOrDefault(row => row.SourceStampId == command.SourceId.Value)
            ?? throw new RegisteredPostingRejectedException("The observed funding claim was not found.");
        var amount = new CoinAmount(CurrencyCode.HardCoin, funding.AuthoritativeUsdMinorUnits);

        command.Authorization.EnsureMatches(
            PostingTemplateKind.ConfirmedTopUpMint,
            command.IdempotencyKey,
            amount,
            command.ReserveVersion,
            command.ConfirmedAt);
        command.Authorization.EnsureSourceRoots([command.SourceId]);

        var providerLeg = new ProviderMonetaryLeg(
            funding.Provider,
            funding.Environment,
            funding.ConnectedAccount,
            funding.ProviderObject,
            funding.ProviderMonetaryLeg);
        var confirmationEventHash = HardCoinFundingClaim.Observe(
                command.SourceId,
                new WalletId(funding.WalletId),
                providerLeg,
                "persistent-observation",
                amount.Units,
                funding.ObservedAt)
            .Transition(SourceConfirmationState.Confirmed, command.Evidence, command.ConfirmedAt)
            .Events[^1]
            .EvidenceHash;
        var sourceContract = new SourceStampContract(
            command.SourceId,
            source.EvidenceHash,
            SourceConfirmationState.Confirmed,
            source.ObservedAt,
            command.ConfirmedAt,
            source.ProviderReference);
        var posting = new PostingRequest(
            command.PostingId,
            new PostingTemplate(PostingTemplateKind.ConfirmedTopUpMint, PostingTemplate.CurrentVersion),
            command.IdempotencyKey,
            PostingAuthority.ProviderConfirmation,
            command.ReserveVersion,
            command.PolicyVersion,
            sourceContract,
            command.ConfirmedAt,
            [
                new PostingLine(1, EntrySide.Debit, EconomyAccountCode.ExternalClearingHard, amount, null, null, null),
                new PostingLine(2, EntrySide.Credit, EconomyAccountCode.PurchasedHardLiability,
                    amount, new WalletId(funding.WalletId), command.CreditLotId, ProvenanceKind.PurchasedHard)
            ]);
        var registered = new RegisteredPostingRequest(request.Authority, posting);
        var payload = RegisteredPostingPayloadFactory.Create(registered, ResolveAccountIds(posting.Lines));

        try
        {
            var receipt = _db.Set<RegisteredPostingReceiptRow>()
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM economy_private.confirm_observed_hard_coin_top_up_v1(
                        {request.Authority.CapabilityId},
                        {request.Authority.ActorId},
                        {request.Authority.TenantId},
                        {posting.Id.Value},
                        {posting.IdempotencyKey.Value},
                        {(int)posting.Template.Kind},
                        {posting.Template.Version},
                        {(int)posting.Authority},
                        {posting.PolicyVersion.Value},
                        {posting.ReserveVersion.Value},
                        {request.Authority.RiskDecisionId},
                        {request.Authority.RiskOperationFingerprint},
                        {request.Authority.ExpectedCounterVersion},
                        {source.Id},
                        {source.EvidenceHash},
                        {posting.RequestedAt},
                        CAST({payload.Lines} AS jsonb),
                        {funding.Version},
                        {command.CreditLotId.Value},
                        {confirmationEventHash},
                        {registered.DispatchSnapshotHash})
                    """)
                .AsNoTracking()
                .Single();

            return new RegisteredPostingReceipt(
                new PostingId(receipt.PostingId),
                receipt.JournalSequence,
                receipt.JournalHash,
                receipt.Duplicate);
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new RegisteredPostingRejectedException(
                "The persistent Economy funding writer rejected the confirmation.",
                exception);
        }
    }

    private IReadOnlyDictionary<int, Guid> ResolveAccountIds(IReadOnlyList<PostingLine> lines)
    {
        var accountIds = new Dictionary<int, Guid>();
        foreach (var line in lines)
        {
            var walletId = line.WalletId?.Value;
            var accountId = _db.Set<EconomyAccountRow>()
                .AsNoTracking()
                .Where(account => account.Code == line.Account &&
                                  account.Currency == line.Amount.Currency &&
                                  account.WalletId == walletId &&
                                  account.Provenance == line.Provenance)
                .Select(account => account.Id)
                .SingleOrDefault();

            if (accountId == Guid.Empty)
                throw new RegisteredPostingRejectedException(
                    "The funding posting references an economy account that is not provisioned.");
            accountIds.Add(line.Sequence, accountId);
        }

        return accountIds;
    }

    private static bool IsDatabaseFailure(Exception exception) =>
        exception is DbException or DbUpdateException or InvalidOperationException ||
        exception.GetBaseException() is DbException;
}

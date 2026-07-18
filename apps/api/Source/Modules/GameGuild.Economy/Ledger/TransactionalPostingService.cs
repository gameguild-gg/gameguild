using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Economy.Contracts;

namespace GameGuild.Economy.Ledger;

public sealed class TransactionalPostingService
{
    private readonly InMemoryLedgerKernelStore _store;
    private readonly IEconomyOutboxFactory _outboxFactory;
    private readonly RootReversalFenceRegistry _fences;

    public TransactionalPostingService(
        InMemoryLedgerKernelStore store,
        IEconomyOutboxFactory? outboxFactory = null,
        RootReversalFenceRegistry? fences = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _outboxFactory = outboxFactory ?? new EconomyOutboxFactory();
        _fences = fences ?? new RootReversalFenceRegistry();
    }

    public SourceEvidence ObserveFunding(ObserveFundingCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _store.Execute(transaction =>
        {
            if (transaction.LatestSource(command.SourceId) is not null)
                throw new InvalidOperationException("Source evidence already exists.");
            var source = SourceEvidence.Observe(
                command.SourceId,
                command.Provider,
                command.ProviderReference,
                command.Evidence,
                command.ObservedAt);
            transaction.AddSource(source);
            return source;
        });
    }

    public PostingResult ConfirmTopUp(ConfirmTopUpCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Amount.Currency != CurrencyCode.HardCoin)
            throw new ArgumentException("Top-ups mint HardCoin only.", nameof(command));

        var commandHash = ComputeTopUpHash(command);
        return _store.Execute(transaction =>
        {
            var duplicate = transaction.FindIdempotent(command.IdempotencyKey, commandHash);
            if (duplicate is not null) return duplicate;

            var observed = transaction.LatestSource(command.SourceId)
                ?? throw new InvalidOperationException("Observed source evidence was not found.");
            var confirmed = observed.Confirm(command.ConfirmedAt);
            transaction.AddSource(confirmed);

            var sourceContract = new SourceStampContract(
                confirmed.Id,
                confirmed.EvidenceHash,
                SourceConfirmationState.Confirmed,
                confirmed.ObservedAt,
                confirmed.ConfirmedAt,
                confirmed.ProviderReference);
            var request = new PostingRequest(
                command.PostingId,
                new PostingTemplate(PostingTemplateKind.ConfirmedTopUpMint, PostingTemplate.CurrentVersion),
                command.IdempotencyKey,
                PostingAuthority.ProviderConfirmation,
                command.ReserveVersion,
                command.PolicyVersion,
                sourceContract,
                command.ConfirmedAt,
                [
                    new PostingLine(1, EntrySide.Debit, EconomyAccountCode.ExternalClearingHard,
                        command.Amount, null, null, null),
                    new PostingLine(2, EntrySide.Credit, EconomyAccountCode.PurchasedHardLiability,
                        command.Amount, command.WalletId, command.CreditLotId, ProvenanceKind.PurchasedHard)
                ]);
            var append = transaction.AppendJournal(request, command.ConfirmedAt);
            var lot = ConfirmedCreditFactory.CreateRootLot(
                command.CreditLotId,
                command.WalletId,
                command.Amount,
                ProvenanceKind.PurchasedHard,
                confirmed,
                command.OriginalMaturesAt,
                append.Entry.Sequence);
            transaction.AddCreditLot(lot);
            transaction.AddProjectionUpdate(new WalletProjectionUpdate(
                command.PostingId,
                command.WalletId,
                command.Amount.Currency,
                command.Amount.Units,
                append.Entry.Sequence));
            transaction.AddIdempotency(new IdempotencyRecord(command.IdempotencyKey, commandHash, append.Result));
            transaction.AddOutbox(_outboxFactory.PostingAccepted(append.Result));
            return append.Result;
        });
    }

    public PostingResult Transfer(TransferFragmentsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SourceWalletId == command.DestinationWalletId)
            throw new ArgumentException("Source and destination wallets must differ.", nameof(command));

        var commandHash = ComputeTransferHash(command);
        var account = LiabilityAccount(command.Amount.Currency, command.Provenance);
        return _store.Execute(transaction =>
        {
            var duplicate = transaction.FindIdempotent(command.IdempotencyKey, commandHash);
            if (duplicate is not null) return duplicate;

            var available = transaction.GetAvailableLots(command.SourceWalletId, command.Amount.Currency)
                .Where(lot => lot.Provenance == command.Provenance)
                .ToArray();
            var selected = FifoFragmentSelector.Select(available, command.Amount);
            var roots = selected.Selections.SelectMany(selection => selection.SelectedRanges).Select(range => range.Root).Distinct().ToArray();
            var snapshot = _fences.Capture(roots);

            return _fences.WithAllocationFence(snapshot, roots, () =>
            {
                var request = new PostingRequest(
                    command.PostingId,
                    new PostingTemplate(PostingTemplateKind.Spend, PostingTemplate.CurrentVersion),
                    command.IdempotencyKey,
                    PostingAuthority.WalletOwner,
                    command.ReserveVersion,
                    command.PolicyVersion,
                    null,
                    command.RequestedAt,
                    [
                        new PostingLine(1, EntrySide.Debit, account, command.Amount,
                            command.SourceWalletId, null, command.Provenance),
                        new PostingLine(2, EntrySide.Credit, account, command.Amount,
                            command.DestinationWalletId, null, command.Provenance)
                    ]);
                var append = transaction.AppendJournal(request, command.RequestedAt);

                for (var index = 0; index < selected.Selections.Count; index++)
                {
                    var selection = selected.Selections[index];
                    var parent = available.Single(lot => lot.Id == selection.ParentLotId);
                    var output = LineageAllocator.CreateDerivedLot(
                        DeterministicLotId(command.PostingId, index),
                        command.DestinationWalletId,
                        parent.Provenance,
                        parent.ConfirmedAt,
                        parent.OriginalMaturesAt,
                        append.Entry.Sequence,
                        [selection],
                        snapshot,
                        _fences);
                    transaction.AddConsumption(new FragmentConsumption(
                        command.PostingId,
                        selection.ParentLotId,
                        selection.Amount,
                        selection.SelectedRanges.ToArray()));
                    transaction.AddCreditLot(output.Lot);
                    transaction.AddLineage(output);
                }

                transaction.AddProjectionUpdate(new WalletProjectionUpdate(
                    command.PostingId, command.SourceWalletId, command.Amount.Currency,
                    -command.Amount.Units, append.Entry.Sequence));
                transaction.AddProjectionUpdate(new WalletProjectionUpdate(
                    command.PostingId, command.DestinationWalletId, command.Amount.Currency,
                    command.Amount.Units, append.Entry.Sequence));
                transaction.AddIdempotency(new IdempotencyRecord(command.IdempotencyKey, commandHash, append.Result));
                transaction.AddOutbox(_outboxFactory.PostingAccepted(append.Result));
                return append.Result;
            });
        });
    }

    private static EconomyAccountCode LiabilityAccount(CurrencyCode currency, ProvenanceKind provenance) =>
        currency switch
        {
            CurrencyCode.HardCoin when provenance == ProvenanceKind.EarnedHard => EconomyAccountCode.EarnedHardLiability,
            CurrencyCode.HardCoin => EconomyAccountCode.PurchasedHardLiability,
            CurrencyCode.SoftCoin => EconomyAccountCode.SoftCoinLiability,
            _ => throw new ArgumentOutOfRangeException(nameof(currency))
        };

    private static CreditLotId DeterministicLotId(PostingId postingId, int index)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{postingId.Value:N}:{index.ToString(CultureInfo.InvariantCulture)}"));
        return new CreditLotId(new Guid(bytes.AsSpan(0, 16)));
    }

    private static string ComputeTopUpHash(ConfirmTopUpCommand command) => Hash(
        command.PostingId.Value.ToString("N"), command.SourceId.Value.ToString("N"),
        command.WalletId.Value.ToString("N"), command.CreditLotId.Value.ToString("N"),
        ((int)command.Amount.Currency).ToString(CultureInfo.InvariantCulture),
        command.Amount.Units.ToString(CultureInfo.InvariantCulture),
        command.ReserveVersion.Value.ToString(CultureInfo.InvariantCulture),
        command.PolicyVersion.Value.ToString(CultureInfo.InvariantCulture),
        command.ConfirmedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
        command.OriginalMaturesAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

    private static string ComputeTransferHash(TransferFragmentsCommand command) => Hash(
        command.PostingId.Value.ToString("N"), command.SourceWalletId.Value.ToString("N"),
        command.DestinationWalletId.Value.ToString("N"),
        ((int)command.Amount.Currency).ToString(CultureInfo.InvariantCulture),
        command.Amount.Units.ToString(CultureInfo.InvariantCulture),
        ((int)command.Provenance).ToString(CultureInfo.InvariantCulture),
        command.ReserveVersion.Value.ToString(CultureInfo.InvariantCulture),
        command.PolicyVersion.Value.ToString(CultureInfo.InvariantCulture),
        command.RequestedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

    private static string Hash(params string[] values)
    {
        var canonical = string.Join('|', values.Select(value => $"{value.Length}:{value}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
